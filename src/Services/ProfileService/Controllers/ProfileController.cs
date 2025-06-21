using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ProfileService.Models;
using ProfileService.Services;
using System.Text.Json;

namespace ProfileService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;
        private readonly IFollowService _followService;
        private readonly IFavoriteService _favoriteService;
        private readonly IExportService _exportService;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IProfileService profileService,
            IFollowService followService,
            IFavoriteService favoriteService,
            IExportService exportService,
            IRabbitMQService rabbitMQService,
            IDistributedCache cache,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _followService = followService;
            _favoriteService = favoriteService;
            _exportService = exportService;
            _rabbitMQService = rabbitMQService;
            _cache = cache;
            _logger = logger;
        }

        [HttpGet("{userId}/profile")]
        public async Task<ActionResult<Profile>> GetProfile(string userId)
        {
            try
            {
                // Try to get from cache first
                var cacheKey = $"profile_{userId}";
                var cachedProfile = await _cache.GetStringAsync(cacheKey);
                
                if (!string.IsNullOrEmpty(cachedProfile))
                {
                    return Ok(JsonSerializer.Deserialize<Profile>(cachedProfile));
                }

                var profile = await _profileService.GetProfileByUserIdAsync(userId);
                if (profile == null)
                {
                    return NotFound(new { message = $"Profile for user {userId} not found." });
                }

                // Cache profile for 5 minutes
                await _cache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(profile),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) }
                );

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving the profile." });
            }
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<Profile>> UpdateProfile([FromBody] ProfileUpdateRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var profile = await _profileService.UpdateProfileAsync(userId, request);
                
                // Invalidate cache
                await _cache.RemoveAsync($"profile_{userId}");
                
                // Publish ProfileUpdated event
                await _rabbitMQService.PublishAsync("profile.events", new ProfileUpdatedEvent
                {
                    UserId = userId,
                    DisplayName = request.DisplayName,
                    Bio = request.Bio,
                    AvatarUrl = request.AvatarUrl,
                    UpdatedAt = DateTime.UtcNow
                });

                return Ok(profile);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while updating the profile." });
            }
        }

        [Authorize]
        [HttpPost("profile/avatar")]
        public async Task<ActionResult<string>> UploadAvatar(IFormFile file)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var avatarUrl = await _profileService.UploadAvatarAsync(userId, file);
                
                // Invalidate cache
                await _cache.RemoveAsync($"profile_{userId}");
                
                return Ok(new { avatarUrl });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnsupportedMediaTypeException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while uploading the avatar." });
            }
        }

        [Authorize]
        [HttpPut("privacy")]
        public async Task<ActionResult<PrivacySettings>> UpdatePrivacySettings([FromBody] PrivacySettings settings)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var updatedSettings = await _profileService.UpdatePrivacySettingsAsync(userId, settings);
                
                // Invalidate cache
                await _cache.RemoveAsync($"profile_{userId}");
                
                return Ok(updatedSettings);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while updating privacy settings." });
            }
        }

        [Authorize]
        [HttpPost("favorites/{modId}")]
        public async Task<ActionResult> AddFavorite(string modId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _favoriteService.AddFavoriteAsync(userId, modId);
                
                // Publish FavoriteAdded event
                await _rabbitMQService.PublishAsync("profile.events", new FavoriteAddedEvent
                {
                    UserId = userId,
                    ModId = modId,
                    AddedAt = DateTime.UtcNow
                });
                
                return Ok(new { message = "Mod added to favorites." });
            }
            catch (AlreadyExistsException)
            {
                return Conflict(new { message = "This mod is already in your favorites." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding favorite {ModId} for user {UserId}", modId, User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while adding the favorite." });
            }
        }

        [Authorize]
        [HttpDelete("favorites/{modId}")]
        public async Task<ActionResult> RemoveFavorite(string modId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _favoriteService.RemoveFavoriteAsync(userId, modId);
                return Ok(new { message = "Mod removed from favorites." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing favorite {ModId} for user {UserId}", modId, User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while removing the favorite." });
            }
        }

        [HttpGet("{userId}/favorites")]
        public async Task<ActionResult<IEnumerable<ModSummary>>> GetUserFavorites(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Check privacy settings if not the user themselves
                if (User.FindFirst("sub")?.Value != userId)
                {
                    var profile = await _profileService.GetProfileByUserIdAsync(userId);
                    if (profile == null || !profile.IsPublic || !profile.PrivacySettings.ShowFavorites)
                    {
                        return Forbid();
                    }
                }

                var favorites = await _favoriteService.GetUserFavoritesAsync(userId, page, pageSize);
                return Ok(favorites);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving favorites." });
            }
        }

        [Authorize]
        [HttpPost("follow/{targetUserId}")]
        public async Task<ActionResult> FollowUser(string targetUserId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                
                // Can't follow yourself
                if (userId == targetUserId)
                {
                    return BadRequest(new { message = "You cannot follow yourself." });
                }

                await _followService.FollowUserAsync(userId, targetUserId);
                return Ok(new { message = $"You are now following user {targetUserId}." });
            }
            catch (AlreadyExistsException)
            {
                return Conflict(new { message = "You are already following this user." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user {TargetUserId} by user {UserId}", targetUserId, User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while following the user." });
            }
        }

        [Authorize]
        [HttpDelete("follow/{targetUserId}")]
        public async Task<ActionResult> UnfollowUser(string targetUserId)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _followService.UnfollowUserAsync(userId, targetUserId);
                return Ok(new { message = $"You have unfollowed user {targetUserId}." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfollowing user {TargetUserId} by user {UserId}", targetUserId, User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while unfollowing the user." });
            }
        }

        [HttpGet("{userId}/followers")]
        public async Task<ActionResult<IEnumerable<ProfileSummary>>> GetFollowers(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Check privacy settings if not the user themselves
                if (User.FindFirst("sub")?.Value != userId)
                {
                    var profile = await _profileService.GetProfileByUserIdAsync(userId);
                    if (profile == null || !profile.IsPublic || !profile.PrivacySettings.ShowFollowers)
                    {
                        return Forbid();
                    }
                }

                var followers = await _followService.GetFollowersAsync(userId, page, pageSize);
                return Ok(followers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving followers for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving followers." });
            }
        }

        [HttpGet("{userId}/following")]
        public async Task<ActionResult<IEnumerable<ProfileSummary>>> GetFollowing(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Check privacy settings if not the user themselves
                if (User.FindFirst("sub")?.Value != userId)
                {
                    var profile = await _profileService.GetProfileByUserIdAsync(userId);
                    if (profile == null || !profile.IsPublic || !profile.PrivacySettings.ShowFollowers)
                    {
                        return Forbid();
                    }
                }

                var following = await _followService.GetFollowingAsync(userId, page, pageSize);
                return Ok(following);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving following for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving following users." });
            }
        }

        [Authorize]
        [HttpGet("downloads/history")]
        public async Task<ActionResult<IEnumerable<DownloadHistoryItem>>> GetDownloadHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var history = await _profileService.GetDownloadHistoryAsync(userId, page, pageSize);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving download history for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving download history." });
            }
        }

        [Authorize]
        [HttpGet("gdpr/export")]
        public async Task<ActionResult> ExportUserData()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                var exportFile = await _exportService.ExportUserDataAsync(userId);
                
                return File(exportFile.Data, "application/json", exportFile.Filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while exporting your data." });
            }
        }

        [Authorize]
        [HttpPost("gdpr/delete-account")]
        public async Task<ActionResult> RequestAccountDeletion()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                await _profileService.RequestAccountDeletionAsync(userId);
                
                // Publish account deletion request event
                await _rabbitMQService.PublishAsync("user.events", new AccountDeletionRequestedEvent
                {
                    UserId = userId,
                    RequestedAt = DateTime.UtcNow
                });
                
                return Ok(new { message = "Account deletion request received. Your account will be processed for deletion." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing account deletion request for user {UserId}", User.FindFirst("sub")?.Value);
                return StatusCode(500, new { message = "An error occurred while processing your account deletion request." });
            }
        }
    }
}

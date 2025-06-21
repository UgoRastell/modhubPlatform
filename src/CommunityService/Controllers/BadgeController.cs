using CommunityService.Models.Badge;
using CommunityService.Services.Badge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CommunityService.Controllers
{
    [ApiController]
    [Route("api/badges")]
    public class BadgeController : ControllerBase
    {
        private readonly IBadgeService _badgeService;
        private readonly ILogger<BadgeController> _logger;

        public BadgeController(IBadgeService badgeService, ILogger<BadgeController> logger)
        {
            _badgeService = badgeService ?? throw new ArgumentNullException(nameof(badgeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Badge Management

        [HttpGet]
        public async Task<ActionResult<List<Badge>>> GetAllBadges()
        {
            try
            {
                var badges = await _badgeService.GetAllBadgesAsync();
                return Ok(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badges");
                return StatusCode(500, "An error occurred while retrieving badges");
            }
        }

        [HttpGet("{badgeId}")]
        public async Task<ActionResult<Badge>> GetBadgeById(string badgeId)
        {
            try
            {
                var badge = await _badgeService.GetBadgeByIdAsync(badgeId);
                
                if (badge == null)
                {
                    return NotFound($"Badge with ID {badgeId} not found");
                }
                
                return Ok(badge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badge {BadgeId}", badgeId);
                return StatusCode(500, "An error occurred while retrieving the badge");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Badge>> CreateBadge([FromBody] Badge badge)
        {
            try
            {
                var createdBadge = await _badgeService.CreateBadgeAsync(badge);
                return CreatedAtAction(nameof(GetBadgeById), new { badgeId = createdBadge.Id }, createdBadge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating badge");
                return StatusCode(500, "An error occurred while creating the badge");
            }
        }

        [HttpPut("{badgeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateBadge(string badgeId, [FromBody] Badge badge)
        {
            try
            {
                if (badgeId != badge.Id)
                {
                    return BadRequest("The badge ID in the URL does not match the ID in the request body");
                }
                
                var success = await _badgeService.UpdateBadgeAsync(badge);
                
                if (!success)
                {
                    return NotFound($"Badge with ID {badgeId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating badge {BadgeId}", badgeId);
                return StatusCode(500, "An error occurred while updating the badge");
            }
        }

        [HttpDelete("{badgeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteBadge(string badgeId)
        {
            try
            {
                var success = await _badgeService.DeleteBadgeAsync(badgeId);
                
                if (!success)
                {
                    return NotFound($"Badge with ID {badgeId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting badge {BadgeId}", badgeId);
                return StatusCode(500, "An error occurred while deleting the badge");
            }
        }

        #endregion

        #region Badge Award Management

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<List<BadgeAward>>> GetUserBadges(string userId)
        {
            try
            {
                var badges = await _badgeService.GetUserBadgesAsync(userId);
                return Ok(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badges for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user badges");
            }
        }

        [HttpGet("users/me")]
        [Authorize]
        public async Task<ActionResult<List<BadgeAward>>> GetMyBadges()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var badges = await _badgeService.GetUserBadgesAsync(currentUserId);
                return Ok(badges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badges for current user");
                return StatusCode(500, "An error occurred while retrieving your badges");
            }
        }

        [HttpPost("award")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AwardBadgeManually([FromBody] AwardBadgeRequest request)
        {
            try
            {
                var award = new BadgeAward
                {
                    BadgeId = request.BadgeId,
                    UserId = request.UserId,
                    AwardedAt = DateTime.UtcNow,
                    AwardedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    Reason = request.Reason
                };

                var success = await _badgeService.AwardBadgeToUserAsync(award);
                
                if (!success)
                {
                    return BadRequest("Failed to award the badge. The badge or user may not exist, or the user may already have this badge.");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding badge {BadgeId} to user {UserId}", request.BadgeId, request.UserId);
                return StatusCode(500, "An error occurred while awarding the badge");
            }
        }

        [HttpDelete("users/{userId}/badges/{badgeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RevokeBadge(string userId, string badgeId, [FromBody] RevokeBadgeRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var success = await _badgeService.RevokeBadgeFromUserAsync(userId, badgeId, adminId, request.Reason);
                
                if (!success)
                {
                    return NotFound("Badge award not found or could not be revoked");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking badge {BadgeId} from user {UserId}", badgeId, userId);
                return StatusCode(500, "An error occurred while revoking the badge");
            }
        }

        #endregion

        #region User Progress and Achievements

        [HttpGet("users/{userId}/progress")]
        public async Task<ActionResult<Dictionary<string, UserBadgeProgress>>> GetUserBadgeProgress(string userId)
        {
            try
            {
                var progress = await _badgeService.GetUserBadgeProgressAsync(userId);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badge progress for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user badge progress");
            }
        }

        [HttpGet("users/me/progress")]
        [Authorize]
        public async Task<ActionResult<Dictionary<string, UserBadgeProgress>>> GetMyBadgeProgress()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var progress = await _badgeService.GetUserBadgeProgressAsync(currentUserId);
                return Ok(progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badge progress for current user");
                return StatusCode(500, "An error occurred while retrieving your badge progress");
            }
        }
        
        [HttpGet("users/{userId}/recent-achievements")]
        public async Task<ActionResult<List<RecentAchievement>>> GetUserRecentAchievements(string userId, [FromQuery] int limit = 10)
        {
            try
            {
                var achievements = await _badgeService.GetUserRecentAchievementsAsync(userId, limit);
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent achievements for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user recent achievements");
            }
        }

        [HttpGet("users/me/recent-achievements")]
        [Authorize]
        public async Task<ActionResult<List<RecentAchievement>>> GetMyRecentAchievements([FromQuery] int limit = 10)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var achievements = await _badgeService.GetUserRecentAchievementsAsync(currentUserId, limit);
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent achievements for current user");
                return StatusCode(500, "An error occurred while retrieving your recent achievements");
            }
        }

        #endregion

        #region Badge Categories and Leaderboards

        [HttpGet("categories")]
        public async Task<ActionResult<List<BadgeCategory>>> GetBadgeCategories()
        {
            try
            {
                var categories = await _badgeService.GetBadgeCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badge categories");
                return StatusCode(500, "An error occurred while retrieving badge categories");
            }
        }

        [HttpGet("leaderboard")]
        public async Task<ActionResult<List<LeaderboardEntry>>> GetBadgeLeaderboard([FromQuery] string? category = null, [FromQuery] int limit = 10)
        {
            try
            {
                var leaderboard = await _badgeService.GetBadgeLeaderboardAsync(category, limit);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving badge leaderboard");
                return StatusCode(500, "An error occurred while retrieving the badge leaderboard");
            }
        }

        #endregion
        
        #region Points and Levels

        [HttpGet("users/{userId}/level")]
        public async Task<ActionResult<UserLevel>> GetUserLevel(string userId)
        {
            try
            {
                var level = await _badgeService.GetUserLevelAsync(userId);
                return Ok(level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving level for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user level");
            }
        }

        [HttpGet("users/me/level")]
        [Authorize]
        public async Task<ActionResult<UserLevel>> GetMyLevel()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var level = await _badgeService.GetUserLevelAsync(currentUserId);
                return Ok(level);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving level for current user");
                return StatusCode(500, "An error occurred while retrieving your level");
            }
        }

        [HttpGet("levels")]
        public async Task<ActionResult<List<LevelDefinition>>> GetLevelDefinitions()
        {
            try
            {
                var levels = await _badgeService.GetLevelDefinitionsAsync();
                return Ok(levels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving level definitions");
                return StatusCode(500, "An error occurred while retrieving level definitions");
            }
        }

        [HttpPost("users/{userId}/points")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AwardPointsToUser(string userId, [FromBody] AwardPointsRequest request)
        {
            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                var pointsAward = new PointsAward
                {
                    UserId = userId,
                    Points = request.Points,
                    Reason = request.Reason,
                    AwardedByUserId = adminId,
                    AwardedAt = DateTime.UtcNow
                };
                
                var success = await _badgeService.AwardPointsToUserAsync(pointsAward);
                
                if (!success)
                {
                    return BadRequest("Failed to award points to the user. The user may not exist.");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error awarding points to user {UserId}", userId);
                return StatusCode(500, "An error occurred while awarding points");
            }
        }

        #endregion

        #region Quests and Challenges

        [HttpGet("challenges")]
        public async Task<ActionResult<List<Challenge>>> GetActiveChallenges()
        {
            try
            {
                var challenges = await _badgeService.GetActiveChallengesAsync();
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active challenges");
                return StatusCode(500, "An error occurred while retrieving active challenges");
            }
        }

        [HttpGet("users/{userId}/challenges")]
        public async Task<ActionResult<List<UserChallengeProgress>>> GetUserChallenges(string userId)
        {
            try
            {
                var challenges = await _badgeService.GetUserChallengesAsync(userId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challenges for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user challenges");
            }
        }

        [HttpGet("users/me/challenges")]
        [Authorize]
        public async Task<ActionResult<List<UserChallengeProgress>>> GetMyChallenges()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var challenges = await _badgeService.GetUserChallengesAsync(currentUserId);
                return Ok(challenges);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challenges for current user");
                return StatusCode(500, "An error occurred while retrieving your challenges");
            }
        }

        [HttpPost("challenges")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Challenge>> CreateChallenge([FromBody] Challenge challenge)
        {
            try
            {
                var createdChallenge = await _badgeService.CreateChallengeAsync(challenge);
                return CreatedAtAction(nameof(GetChallengeById), new { challengeId = createdChallenge.Id }, createdChallenge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating challenge");
                return StatusCode(500, "An error occurred while creating the challenge");
            }
        }

        [HttpGet("challenges/{challengeId}")]
        public async Task<ActionResult<Challenge>> GetChallengeById(string challengeId)
        {
            try
            {
                var challenge = await _badgeService.GetChallengeByIdAsync(challengeId);
                
                if (challenge == null)
                {
                    return NotFound($"Challenge with ID {challengeId} not found");
                }
                
                return Ok(challenge);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving challenge {ChallengeId}", challengeId);
                return StatusCode(500, "An error occurred while retrieving the challenge");
            }
        }

        #endregion
    }

    public class AwardBadgeRequest
    {
        public string BadgeId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class RevokeBadgeRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AwardPointsRequest
    {
        public int Points { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}

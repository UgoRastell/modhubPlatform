using CommunityService.Models.Forum;
using CommunityService.Services.Forum;
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
    [Route("api/forum/topics")]
    public class ForumTopicController : ControllerBase
    {
        private readonly IForumService _forumService;
        private readonly ILogger<ForumTopicController> _logger;

        public ForumTopicController(IForumService forumService, ILogger<ForumTopicController> logger)
        {
            _forumService = forumService ?? throw new ArgumentNullException(nameof(forumService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<ForumTopic>>> GetAllTopics([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var topics = await _forumService.GetAllTopicsAsync(page, pageSize);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum topics");
                return StatusCode(500, "An error occurred while retrieving forum topics");
            }
        }

        [HttpGet("{topicId}")]
        public async Task<ActionResult<ForumTopic>> GetTopicById(string topicId)
        {
            try
            {
                var topic = await _forumService.GetTopicByIdAsync(topicId);
                
                if (topic == null)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                // Incrémenter le nombre de vues
                await _forumService.IncrementTopicViewCountAsync(topicId);
                
                return Ok(topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while retrieving the forum topic");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ForumTopic>> CreateTopic([FromBody] CreateTopicRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var topic = new ForumTopic
                {
                    Title = request.Title,
                    Content = request.Content,
                    CategoryId = request.CategoryId,
                    AuthorId = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    IsPinned = false,
                    IsLocked = false,
                    ViewCount = 0,
                    Tags = request.Tags ?? new List<string>()
                };

                var createdTopic = await _forumService.CreateTopicAsync(topic);
                return CreatedAtAction(nameof(GetTopicById), new { topicId = createdTopic.Id }, createdTopic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum topic");
                return StatusCode(500, "An error occurred while creating the forum topic");
            }
        }

        [HttpPut("{topicId}")]
        [Authorize]
        public async Task<ActionResult> UpdateTopic(string topicId, [FromBody] UpdateTopicRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var existingTopic = await _forumService.GetTopicByIdAsync(topicId);
                
                if (existingTopic == null)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                // Vérifier si l'utilisateur est l'auteur du sujet ou un modérateur/admin
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderator");
                bool isAuthor = existingTopic.AuthorId == currentUserId;
                
                if (!isAdmin && !isModerator && !isAuthor)
                {
                    return Forbid("You do not have permission to edit this topic");
                }
                
                // Mettre à jour uniquement les champs autorisés
                existingTopic.Title = request.Title ?? existingTopic.Title;
                existingTopic.Content = request.Content ?? existingTopic.Content;
                
                // Seuls les modérateurs/admin peuvent changer ces propriétés
                if (isAdmin || isModerator)
                {
                    if (request.CategoryId != null)
                    {
                        existingTopic.CategoryId = request.CategoryId;
                    }
                    
                    if (request.IsPinned.HasValue)
                    {
                        existingTopic.IsPinned = request.IsPinned.Value;
                    }
                    
                    if (request.IsLocked.HasValue)
                    {
                        existingTopic.IsLocked = request.IsLocked.Value;
                    }
                }
                
                // Les tags peuvent être édités par l'auteur ou les modérateurs
                if (request.Tags != null)
                {
                    existingTopic.Tags = request.Tags;
                }
                
                existingTopic.LastActivityAt = DateTime.UtcNow;
                existingTopic.EditedAt = DateTime.UtcNow;
                existingTopic.EditedByUserId = currentUserId;
                
                var success = await _forumService.UpdateTopicAsync(existingTopic);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to update the topic");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating forum topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while updating the forum topic");
            }
        }

        [HttpDelete("{topicId}")]
        [Authorize]
        public async Task<ActionResult> DeleteTopic(string topicId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }
                
                var existingTopic = await _forumService.GetTopicByIdAsync(topicId);
                
                if (existingTopic == null)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                // Vérifier si l'utilisateur est l'auteur du sujet ou un modérateur/admin
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderator");
                bool isAuthor = existingTopic.AuthorId == currentUserId;
                
                if (!isAdmin && !isModerator && !isAuthor)
                {
                    return Forbid("You do not have permission to delete this topic");
                }
                
                var success = await _forumService.DeleteTopicAsync(topicId);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to delete the topic");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forum topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while deleting the forum topic");
            }
        }

        [HttpGet("{topicId}/posts")]
        public async Task<ActionResult<List<ForumPost>>> GetPostsByTopic(string topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var posts = await _forumService.GetPostsByTopicAsync(topicId, page, pageSize);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving posts for topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while retrieving posts");
            }
        }

        [HttpPost("{topicId}/pin")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> PinTopic(string topicId)
        {
            try
            {
                var success = await _forumService.SetTopicPinStatusAsync(topicId, true);
                
                if (!success)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pinning topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while pinning the topic");
            }
        }

        [HttpPost("{topicId}/unpin")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UnpinTopic(string topicId)
        {
            try
            {
                var success = await _forumService.SetTopicPinStatusAsync(topicId, false);
                
                if (!success)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unpinning topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while unpinning the topic");
            }
        }

        [HttpPost("{topicId}/lock")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> LockTopic(string topicId)
        {
            try
            {
                var success = await _forumService.SetTopicLockStatusAsync(topicId, true);
                
                if (!success)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while locking the topic");
            }
        }

        [HttpPost("{topicId}/unlock")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UnlockTopic(string topicId)
        {
            try
            {
                var success = await _forumService.SetTopicLockStatusAsync(topicId, false);
                
                if (!success)
                {
                    return NotFound($"Topic with ID {topicId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking topic {TopicId}", topicId);
                return StatusCode(500, "An error occurred while unlocking the topic");
            }
        }
        
        [HttpGet("search")]
        public async Task<ActionResult<List<ForumTopic>>> SearchTopics([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Si la requête est vide, on interprète cela comme une demande des derniers sujets.
                // On conserve la validation > 3 caractères uniquement si la requête n'est pas vide.
                if (!string.IsNullOrWhiteSpace(query) && query.Length < 3)
                {
                    return BadRequest("Search query must be at least 3 characters long");
                }
                
                var results = await _forumService.SearchTopicsAsync(query, page, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching forum topics for query {Query}", query);
                return StatusCode(500, "An error occurred while searching forum topics");
            }
        }
    }
    
    public class CreateTopicRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public List<string>? Tags { get; set; }
    }
    
    public class UpdateTopicRequest
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? CategoryId { get; set; }
        public bool? IsPinned { get; set; }
        public bool? IsLocked { get; set; }
        public List<string>? Tags { get; set; }
    }
}

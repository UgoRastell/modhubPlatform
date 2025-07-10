using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MongoDB.Driver;
using CommunityService.Models.Forum;

namespace CommunityService.Controllers
{
    [ApiController]
    [Route("api/forum")]
    public class ForumTopicController : ControllerBase
    {
        private readonly IMongoCollection<ForumTopic> _topics;

        public ForumTopicController(IMongoDatabase database)
        {
            _topics = database.GetCollection<ForumTopic>("forum_topics");
        }

        /// <summary>
        /// Recherche de sujets dans le forum (accès public).
        /// Pour l'instant renvoie une collection vide tant que l'implémentation du service n'est pas prête.
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<ForumTopicSearchViewModel>>> SearchTopics(
            [FromQuery] string query = "",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
                        var filter = Builders<ForumTopic>.Filter.Empty;
            if (!string.IsNullOrWhiteSpace(query))
            {
                filter = Builders<ForumTopic>.Filter.Text(query);
            }
            var totalItems = await _topics.CountDocumentsAsync(filter);
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var topicsCursor = await _topics.Find(filter)
                                            .SortByDescending(t => t.CreatedAt)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();
            var items = topicsCursor.Select(t => new ForumTopicSearchViewModel
            {
                Id = t.Id,
                Title = t.Title,
                AuthorName = t.CreatedByUsername,
                CreatedAt = t.CreatedAt,
                RepliesCount = t.Posts.Count - 1, // first post is topic
                CategoryName = "" // category not stored yet
            }).ToList();

            var result = new PagedResult<ForumTopicSearchViewModel>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = (int)totalItems,
                TotalPages = totalPages
            };
            return Ok(result);
        }

                /// <summary>
        /// Crée un nouveau topic avec le premier message.
        /// </summary>
        [HttpPost("topics")]
        [Authorize]
        public async Task<ActionResult<ForumTopic>> CreateTopic([FromBody] CreateSimpleTopicDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Utilisateur";
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var post = new ForumPost
            {
                Content = dto.Content,
                CreatedByUserId = userId,
                CreatedByUsername = username
            };

            var topic = new ForumTopic
            {
                Title = dto.Title,
                CreatedByUserId = userId,
                CreatedByUsername = username,
                Posts = new List<ForumPost> { post }
            };
            await _topics.InsertOneAsync(topic);
            return CreatedAtAction(nameof(GetTopicById), new { id = topic.Id }, topic);
        }

        [HttpGet("topics/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ForumTopic>> GetTopicById(string id)
        {
            var topic = await _topics.Find(t => t.Id == id).FirstOrDefaultAsync();
            if (topic == null) return NotFound();
            return topic;
        }

#region internal DTOs (temporaire)
        public class ForumTopicSearchViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int RepliesCount { get; set; }
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalItems { get; set; }
            public int TotalPages { get; set; }
        }
        #endregion
    }
}

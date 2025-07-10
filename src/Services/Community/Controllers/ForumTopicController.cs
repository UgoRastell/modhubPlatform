using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using MongoDB.Driver;
using CommunityService.Models.Forum;

namespace CommunityService.Controllers
{
    [ApiController]
    [Route("api/forum")]
    [AllowAnonymous]
    public class ForumTopicController : ControllerBase
    {
        private readonly IMongoCollection<ForumTopic> _topics;

        public ForumTopicController(IMongoDatabase database)
        {
            _topics = database.GetCollection<ForumTopic>("forum_topics");
            EnsureIndexes();
        }

        private static bool _indexesEnsured = false;
        private void EnsureIndexes()
        {
            if (_indexesEnsured) return;
            try
            {
                var indexKeys = Builders<ForumTopic>.IndexKeys.Text(t => t.Title).Text("posts.content");
                var model = new CreateIndexModel<ForumTopic>(indexKeys, new CreateIndexOptions { Name = "TopicTextIndex" });
                _topics.Indexes.CreateOne(model);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict")
            {
                // Index already exists with different options – ignore.
            }
            catch
            {
                // Ignore any other errors during index creation to avoid blocking startup.
            }
            _indexesEnsured = true;
        }

        /// <summary>
        /// Recherche de sujets dans le forum (accès public).
        /// Pour l'instant renvoie une collection vide tant que l'implémentation du service n'est pas prête.
        /// </summary>
        [HttpGet("search")]
        // hérite de [AllowAnonymous] du contrôleur
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
                            long totalItems = 0;
                List<ForumTopic> topicsCursor = new();
                try
                {
                    totalItems = await _topics.CountDocumentsAsync(filter);
                    topicsCursor = await _topics.Find(filter)
                                               .SortByDescending(t => t.CreatedAt)
                                               .Skip((page - 1) * pageSize)
                                               .Limit(pageSize)
                                               .ToListAsync();
                }
                catch (System.EntryPointNotFoundException)
                {
                    // Probablement index absent ou collection non initialisée : retourner liste vide pour éviter 500
                    totalItems = 0;
                    topicsCursor = new List<ForumTopic>();
                }
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

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
                Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                Content = dto.Content,
                CreatedByUserId = userId,
                CreatedByUsername = username
            };

            var topic = new ForumTopic
            {
                // Laisse Id null pour que MongoDB génère _id automatiquement
                Id = null,
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

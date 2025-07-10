using CommunityService.Models.Forum;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;

namespace CommunityService.Controllers;

[ApiController]
[Route("api/forum")]
public class ForumPostController : ControllerBase
{
    private readonly IMongoCollection<ForumTopic> _topics;

    public ForumPostController(IMongoDatabase database)
    {
        _topics = database.GetCollection<ForumTopic>("forum_topics");
    }

    /// <summary>
    /// Récupère la liste des posts d'un topic (pagination simple).
    /// </summary>
    [HttpGet("topics/{topicId}/posts")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ForumPost>>> GetPostsByTopic(string topicId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var topic = await _topics.Find(t => t.Id == topicId).FirstOrDefaultAsync();
        if (topic is null)
            return NotFound();

        var skip = (page - 1) * pageSize;
        var posts = topic.Posts.OrderBy(p => p.CreatedAt).Skip(skip).Take(pageSize).ToList();
        var view = posts.Select(p => new {
            id = p.Id,
            topicId = topicId,
            content = p.Content,
            authorId = p.CreatedByUserId,
            authorName = p.CreatedByUsername,
            createdAt = p.CreatedAt
        });
        return Ok(view);
    }

    /// <summary>
    /// Ajoute un nouveau post dans un topic existant.
    /// </summary>
    [HttpPost("posts")]
    [Authorize]
    public async Task<ActionResult<ForumPost>> CreatePost([FromBody] CreateForumPostDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Utilisateur";
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var topic = await _topics.Find(t => t.Id == dto.TopicId).FirstOrDefaultAsync();
        if (topic is null)
            return NotFound("Topic not found");

        var post = new ForumPost
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Content = dto.Content,
            CreatedByUserId = userId,
            CreatedByUsername = username,
            CreatedAt = DateTime.UtcNow,
            ParentPostId = dto.ParentPostId
        };

        var update = Builders<ForumTopic>.Update.Push(t => t.Posts, post);
        await _topics.UpdateOneAsync(t => t.Id == dto.TopicId, update);

        var view = new {
            id = post.Id,
            topicId = dto.TopicId,
            content = post.Content,
            authorId = post.CreatedByUserId,
            authorName = post.CreatedByUsername,
            createdAt = post.CreatedAt
        };
        return Created($"api/forum/posts/{post.Id}", view);
    }

    #region DTO internal (temporaire)
    public class CreateForumPostDto
    {
        public string TopicId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ParentPostId { get; set; }
        
    }
    #endregion
}

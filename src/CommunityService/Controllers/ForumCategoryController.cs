using CommunityService.Models.Forum;
using CommunityService.Services.Forum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunityService.Controllers
{
    [ApiController]
    [Route("api/forum/categories")]
    public class ForumCategoryController : ControllerBase
    {
        private readonly IForumService _forumService;
        private readonly ILogger<ForumCategoryController> _logger;

        public ForumCategoryController(IForumService forumService, ILogger<ForumCategoryController> logger)
        {
            _forumService = forumService ?? throw new ArgumentNullException(nameof(forumService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<ActionResult<List<ForumCategory>>> GetAllCategories()
        {
            try
            {
                var categories = await _forumService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum categories");
                return StatusCode(500, "An error occurred while retrieving forum categories");
            }
        }

        [HttpGet("{categoryId}")]
        public async Task<ActionResult<ForumCategory>> GetCategoryById(string categoryId)
        {
            try
            {
                var category = await _forumService.GetCategoryByIdAsync(categoryId);
                
                if (category == null)
                {
                    return NotFound($"Category with ID {categoryId} not found");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while retrieving the forum category");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<ForumCategory>> CreateCategory([FromBody] ForumCategory category)
        {
            try
            {
                var createdCategory = await _forumService.CreateCategoryAsync(category);
                return CreatedAtAction(nameof(GetCategoryById), new { categoryId = createdCategory.Id }, createdCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum category");
                return StatusCode(500, "An error occurred while creating the forum category");
            }
        }

        [HttpPut("{categoryId}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UpdateCategory(string categoryId, [FromBody] ForumCategory category)
        {
            try
            {
                if (categoryId != category.Id)
                {
                    return BadRequest("The category ID in the URL does not match the ID in the request body");
                }
                
                var success = await _forumService.UpdateCategoryAsync(category);
                
                if (!success)
                {
                    return NotFound($"Category with ID {categoryId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating forum category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while updating the forum category");
            }
        }

        [HttpDelete("{categoryId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteCategory(string categoryId)
        {
            try
            {
                var success = await _forumService.DeleteCategoryAsync(categoryId);
                
                if (!success)
                {
                    return NotFound($"Category with ID {categoryId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forum category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while deleting the forum category");
            }
        }
        
        [HttpGet("{categoryId}/topics")]
        public async Task<ActionResult<List<ForumTopic>>> GetTopicsByCategory(string categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var topics = await _forumService.GetTopicsByCategoryAsync(categoryId, page, pageSize);
                return Ok(topics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving topics for category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while retrieving topics");
            }
        }
        
        [HttpPut("{categoryId}/order")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UpdateCategoryOrder(string categoryId, [FromBody] UpdateOrderRequest request)
        {
            try
            {
                var success = await _forumService.UpdateCategoryOrderAsync(categoryId, request.NewOrder);
                
                if (!success)
                {
                    return NotFound($"Category with ID {categoryId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order for category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while updating the category order");
            }
        }
    }
    
    public class UpdateOrderRequest
    {
        public int NewOrder { get; set; }
    }
}

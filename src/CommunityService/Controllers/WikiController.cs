using CommunityService.Models.Wiki;
using CommunityService.Services.Wiki;
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
    [Route("api/wiki")]
    public class WikiController : ControllerBase
    {
        private readonly IWikiService _wikiService;
        private readonly ILogger<WikiController> _logger;

        public WikiController(IWikiService wikiService, ILogger<WikiController> logger)
        {
            _wikiService = wikiService ?? throw new ArgumentNullException(nameof(wikiService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Page Management

        [HttpGet]
        public async Task<ActionResult<List<WikiPageSummary>>> GetAllPages([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var pages = await _wikiService.GetAllPagesAsync(page, pageSize);
                return Ok(pages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wiki pages");
                return StatusCode(500, "An error occurred while retrieving wiki pages");
            }
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<WikiPage>> GetPageBySlug(string slug)
        {
            try
            {
                var page = await _wikiService.GetPageBySlugAsync(slug);
                
                if (page == null)
                {
                    return NotFound($"Wiki page with slug '{slug}' not found");
                }
                
                // Incrémenter le nombre de vues
                await _wikiService.IncrementPageViewCountAsync(page.Id);
                
                return Ok(page);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wiki page with slug {Slug}", slug);
                return StatusCode(500, "An error occurred while retrieving the wiki page");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<WikiPage>> CreatePage([FromBody] CreateWikiPageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var page = new WikiPage
                {
                    Title = request.Title,
                    Slug = request.Slug,
                    Content = request.Content,
                    ParentPageId = request.ParentPageId,
                    CreatedByUserId = currentUserId,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow,
                    CategoryIds = request.CategoryIds ?? new List<string>(),
                    Tags = request.Tags ?? new List<string>(),
                    Status = request.Status
                };

                var createdPage = await _wikiService.CreatePageAsync(page);
                
                // Enregistrer la première révision
                await _wikiService.CreatePageRevisionAsync(new WikiPageRevision
                {
                    PageId = createdPage.Id,
                    Title = createdPage.Title,
                    Content = createdPage.Content,
                    UserId = currentUserId,
                    RevisionNumber = 1,
                    CreatedAt = DateTime.UtcNow,
                    Comment = "Initial version"
                });
                
                return CreatedAtAction(nameof(GetPageBySlug), new { slug = createdPage.Slug }, createdPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wiki page");
                return StatusCode(500, "An error occurred while creating the wiki page");
            }
        }

        [HttpPut("{pageId}")]
        [Authorize]
        public async Task<ActionResult> UpdatePage(string pageId, [FromBody] UpdateWikiPageRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var existingPage = await _wikiService.GetPageByIdAsync(pageId);
                
                if (existingPage == null)
                {
                    return NotFound($"Wiki page with ID {pageId} not found");
                }

                // Vérifier les autorisations
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderator");
                bool canEdit = isAdmin || isModerator || await _wikiService.CanUserEditPageAsync(currentUserId, pageId);
                
                if (!canEdit)
                {
                    return Forbid("You do not have permission to edit this wiki page");
                }
                
                // Créer une nouvelle révision avant la mise à jour
                await _wikiService.CreatePageRevisionAsync(new WikiPageRevision
                {
                    PageId = pageId,
                    Title = existingPage.Title,
                    Content = existingPage.Content,
                    UserId = currentUserId,
                    RevisionNumber = await _wikiService.GetNextRevisionNumberAsync(pageId),
                    CreatedAt = DateTime.UtcNow,
                    Comment = request.RevisionComment ?? "Updated page"
                });
                
                // Mettre à jour la page
                existingPage.Title = request.Title ?? existingPage.Title;
                existingPage.Content = request.Content ?? existingPage.Content;
                existingPage.LastUpdatedAt = DateTime.UtcNow;
                existingPage.LastUpdatedByUserId = currentUserId;
                
                // Ces champs ne peuvent être mis à jour que par les administrateurs ou modérateurs
                if (isAdmin || isModerator)
                {
                    if (request.Slug != null)
                    {
                        existingPage.Slug = request.Slug;
                    }
                    
                    if (request.ParentPageId != null)
                    {
                        existingPage.ParentPageId = request.ParentPageId;
                    }
                    
                    if (request.Status.HasValue)
                    {
                        existingPage.Status = request.Status.Value;
                    }
                }
                
                // Ces champs peuvent être modifiés par tout utilisateur autorisé à éditer
                if (request.CategoryIds != null)
                {
                    existingPage.CategoryIds = request.CategoryIds;
                }
                
                if (request.Tags != null)
                {
                    existingPage.Tags = request.Tags;
                }
                
                var success = await _wikiService.UpdatePageAsync(existingPage);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to update the wiki page");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating wiki page {PageId}", pageId);
                return StatusCode(500, "An error occurred while updating the wiki page");
            }
        }

        [HttpDelete("{pageId}")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> DeletePage(string pageId)
        {
            try
            {
                var success = await _wikiService.DeletePageAsync(pageId);
                
                if (!success)
                {
                    return NotFound($"Wiki page with ID {pageId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting wiki page {PageId}", pageId);
                return StatusCode(500, "An error occurred while deleting the wiki page");
            }
        }
        
        #endregion
        
        #region Revisions
        
        [HttpGet("{pageId}/revisions")]
        public async Task<ActionResult<List<WikiPageRevision>>> GetPageRevisions(string pageId)
        {
            try
            {
                var revisions = await _wikiService.GetPageRevisionsAsync(pageId);
                return Ok(revisions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revisions for page {PageId}", pageId);
                return StatusCode(500, "An error occurred while retrieving page revisions");
            }
        }
        
        [HttpGet("{pageId}/revisions/{revisionNumber}")]
        public async Task<ActionResult<WikiPageRevision>> GetPageRevision(string pageId, int revisionNumber)
        {
            try
            {
                var revision = await _wikiService.GetPageRevisionAsync(pageId, revisionNumber);
                
                if (revision == null)
                {
                    return NotFound($"Revision {revisionNumber} for page {pageId} not found");
                }
                
                return Ok(revision);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revision {RevisionNumber} for page {PageId}", revisionNumber, pageId);
                return StatusCode(500, "An error occurred while retrieving the page revision");
            }
        }
        
        [HttpPost("{pageId}/restore/{revisionNumber}")]
        [Authorize]
        public async Task<ActionResult> RestorePageRevision(string pageId, int revisionNumber)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Vérifier les autorisations
                bool canEdit = User.IsInRole("Admin") || User.IsInRole("Moderator") || 
                               await _wikiService.CanUserEditPageAsync(currentUserId, pageId);
                
                if (!canEdit)
                {
                    return Forbid("You do not have permission to restore revisions for this wiki page");
                }
                
                var success = await _wikiService.RestorePageRevisionAsync(pageId, revisionNumber, currentUserId);
                
                if (!success)
                {
                    return NotFound($"Revision {revisionNumber} for page {pageId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring revision {RevisionNumber} for page {PageId}", revisionNumber, pageId);
                return StatusCode(500, "An error occurred while restoring the page revision");
            }
        }
        
        #endregion
        
        #region Categories and Navigation
        
        [HttpGet("categories")]
        public async Task<ActionResult<List<WikiCategory>>> GetAllCategories()
        {
            try
            {
                var categories = await _wikiService.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wiki categories");
                return StatusCode(500, "An error occurred while retrieving wiki categories");
            }
        }
        
        [HttpPost("categories")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<WikiCategory>> CreateCategory([FromBody] WikiCategory category)
        {
            try
            {
                var createdCategory = await _wikiService.CreateCategoryAsync(category);
                return CreatedAtAction(nameof(GetCategoryById), new { categoryId = createdCategory.Id }, createdCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wiki category");
                return StatusCode(500, "An error occurred while creating the wiki category");
            }
        }
        
        [HttpGet("categories/{categoryId}")]
        public async Task<ActionResult<WikiCategory>> GetCategoryById(string categoryId)
        {
            try
            {
                var category = await _wikiService.GetCategoryByIdAsync(categoryId);
                
                if (category == null)
                {
                    return NotFound($"Wiki category with ID {categoryId} not found");
                }
                
                return Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wiki category {CategoryId}", categoryId);
                return StatusCode(500, "An error occurred while retrieving the wiki category");
            }
        }
        
        [HttpGet("structure")]
        public async Task<ActionResult<WikiStructure>> GetWikiStructure()
        {
            try
            {
                var structure = await _wikiService.GetWikiStructureAsync();
                return Ok(structure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wiki structure");
                return StatusCode(500, "An error occurred while retrieving the wiki structure");
            }
        }
        
        #endregion
        
        #region Search
        
        [HttpGet("search")]
        public async Task<ActionResult<List<WikiSearchResult>>> SearchWiki([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
                {
                    return BadRequest("Search query must be at least 3 characters long");
                }
                
                var results = await _wikiService.SearchWikiAsync(query, page, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching wiki for query {Query}", query);
                return StatusCode(500, "An error occurred while searching the wiki");
            }
        }
        
        #endregion
    }
    
    public class CreateWikiPageRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ParentPageId { get; set; }
        public List<string>? CategoryIds { get; set; }
        public List<string>? Tags { get; set; }
        public WikiPageStatus Status { get; set; } = WikiPageStatus.Draft;
    }
    
    public class UpdateWikiPageRequest
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Content { get; set; }
        public string? ParentPageId { get; set; }
        public List<string>? CategoryIds { get; set; }
        public List<string>? Tags { get; set; }
        public WikiPageStatus? Status { get; set; }
        public string? RevisionComment { get; set; }
    }
}

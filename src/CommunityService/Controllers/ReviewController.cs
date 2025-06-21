using CommunityService.Models.Review;
using CommunityService.Services.Review;
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
    [Route("api/reviews")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
        {
            _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Mod Reviews

        [HttpGet("mods/{modId}")]
        public async Task<ActionResult<List<ModReview>>> GetReviewsByMod(string modId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] ReviewSortOption sort = ReviewSortOption.Newest)
        {
            try
            {
                var reviews = await _reviewService.GetModReviewsAsync(modId, page, pageSize, sort);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for mod {ModId}", modId);
                return StatusCode(500, "An error occurred while retrieving reviews");
            }
        }

        [HttpGet("mods/{modId}/summary")]
        public async Task<ActionResult<ReviewSummary>> GetModReviewSummary(string modId)
        {
            try
            {
                var summary = await _reviewService.GetModReviewSummaryAsync(modId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review summary for mod {ModId}", modId);
                return StatusCode(500, "An error occurred while retrieving the review summary");
            }
        }

        [HttpPost("mods/{modId}")]
        [Authorize]
        public async Task<ActionResult<ModReview>> CreateModReview(string modId, [FromBody] CreateModReviewRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Vérifier si l'utilisateur a déjà laissé un avis
                var existingReview = await _reviewService.GetUserModReviewAsync(modId, currentUserId);
                if (existingReview != null)
                {
                    return Conflict("You have already reviewed this mod. Please update your existing review instead.");
                }

                var review = new ModReview
                {
                    ModId = modId,
                    UserId = currentUserId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                var createdReview = await _reviewService.CreateModReviewAsync(review);
                return CreatedAtAction(nameof(GetReviewById), new { reviewId = createdReview.Id }, createdReview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for mod {ModId}", modId);
                return StatusCode(500, "An error occurred while creating the review");
            }
        }

        [HttpGet("{reviewId}")]
        public async Task<ActionResult<ModReview>> GetReviewById(string reviewId)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(reviewId);
                
                if (review == null)
                {
                    return NotFound($"Review with ID {reviewId} not found");
                }
                
                return Ok(review);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while retrieving the review");
            }
        }

        [HttpPut("{reviewId}")]
        [Authorize]
        public async Task<ActionResult> UpdateReview(string reviewId, [FromBody] UpdateModReviewRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var existingReview = await _reviewService.GetReviewByIdAsync(reviewId);
                
                if (existingReview == null)
                {
                    return NotFound($"Review with ID {reviewId} not found");
                }
                
                // Vérifier si l'utilisateur est l'auteur de l'avis ou un modérateur/admin
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderator");
                bool isAuthor = existingReview.UserId == currentUserId;
                
                if (!isAdmin && !isModerator && !isAuthor)
                {
                    return Forbid("You do not have permission to update this review");
                }
                
                // Mise à jour de l'avis
                existingReview.Rating = request.Rating ?? existingReview.Rating;
                existingReview.Comment = request.Comment ?? existingReview.Comment;
                existingReview.LastUpdatedAt = DateTime.UtcNow;
                
                var success = await _reviewService.UpdateModReviewAsync(existingReview);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to update the review");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while updating the review");
            }
        }

        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<ActionResult> DeleteReview(string reviewId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var existingReview = await _reviewService.GetReviewByIdAsync(reviewId);
                
                if (existingReview == null)
                {
                    return NotFound($"Review with ID {reviewId} not found");
                }
                
                // Vérifier si l'utilisateur est l'auteur de l'avis ou un modérateur/admin
                bool isAdmin = User.IsInRole("Admin");
                bool isModerator = User.IsInRole("Moderator");
                bool isAuthor = existingReview.UserId == currentUserId;
                
                if (!isAdmin && !isModerator && !isAuthor)
                {
                    return Forbid("You do not have permission to delete this review");
                }
                
                var success = await _reviewService.DeleteModReviewAsync(reviewId);
                
                if (!success)
                {
                    return StatusCode(500, "Failed to delete the review");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while deleting the review");
            }
        }

        #endregion

        #region Comment Votes

        [HttpPost("{reviewId}/vote")]
        [Authorize]
        public async Task<ActionResult> VoteOnReview(string reviewId, [FromBody] ReviewVoteRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var success = await _reviewService.AddReviewVoteAsync(reviewId, currentUserId, request.IsHelpful);
                
                if (!success)
                {
                    return NotFound($"Review with ID {reviewId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error voting on review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while voting on the review");
            }
        }

        [HttpDelete("{reviewId}/vote")]
        [Authorize]
        public async Task<ActionResult> RemoveVoteFromReview(string reviewId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var success = await _reviewService.RemoveReviewVoteAsync(reviewId, currentUserId);
                
                if (!success)
                {
                    return NotFound($"Vote for review with ID {reviewId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing vote from review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while removing the vote");
            }
        }

        #endregion

        #region Reporting

        [HttpPost("{reviewId}/report")]
        [Authorize]
        public async Task<ActionResult> ReportReview(string reviewId, [FromBody] ReportReviewRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var report = new ReviewReport
                {
                    ReviewId = reviewId,
                    ReportedByUserId = currentUserId,
                    Reason = request.Reason,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    Status = ReportStatus.Pending
                };

                var success = await _reviewService.ReportReviewAsync(report);
                
                if (!success)
                {
                    return NotFound($"Review with ID {reviewId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting review {ReviewId}", reviewId);
                return StatusCode(500, "An error occurred while reporting the review");
            }
        }

        [HttpGet("reports")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<List<ReviewReport>>> GetReviewReports([FromQuery] ReportStatus? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var reports = await _reviewService.GetReviewReportsAsync(status, page, pageSize);
                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving review reports");
                return StatusCode(500, "An error occurred while retrieving review reports");
            }
        }

        [HttpPut("reports/{reportId}/resolve")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> ResolveReport(string reportId, [FromBody] ResolveReportRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var success = await _reviewService.ResolveReportAsync(reportId, request.Resolution, request.Comment, currentUserId);
                
                if (!success)
                {
                    return NotFound($"Report with ID {reportId} not found");
                }
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving report {ReportId}", reportId);
                return StatusCode(500, "An error occurred while resolving the report");
            }
        }

        #endregion

        #region User Reviews

        [HttpGet("users/{userId}")]
        public async Task<ActionResult<List<ModReview>>> GetReviewsByUser(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var reviews = await _reviewService.GetUserReviewsAsync(userId, page, pageSize);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving user reviews");
            }
        }

        [HttpGet("users/me")]
        [Authorize]
        public async Task<ActionResult<List<ModReview>>> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized("User not authenticated");
                }

                var reviews = await _reviewService.GetUserReviewsAsync(currentUserId, page, pageSize);
                return Ok(reviews);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reviews for current user");
                return StatusCode(500, "An error occurred while retrieving your reviews");
            }
        }

        #endregion
    }

    public class CreateModReviewRequest
    {
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }

    public class UpdateModReviewRequest
    {
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewVoteRequest
    {
        public bool IsHelpful { get; set; }
    }

    public class ReportReviewRequest
    {
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ResolveReportRequest
    {
        public ReportResolution Resolution { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}

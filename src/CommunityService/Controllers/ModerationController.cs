using CommunityService.Models.Moderation;
using CommunityService.Services.Moderation;
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
    [Route("api/moderation")]
    public class ModerationController : ControllerBase
    {
        private readonly IContentReportingService _reportingService;
        private readonly ILogger<ModerationController> _logger;

        public ModerationController(IContentReportingService reportingService, ILogger<ModerationController> logger)
        {
            _reportingService = reportingService ?? throw new ArgumentNullException(nameof(reportingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Signalement de contenu (utilisateurs)

        [HttpPost("reports")]
        [Authorize]
        public async Task<ActionResult<ContentReport>> ReportContent([FromBody] CreateReportRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Utilisateur inconnu";

                // Vérifier si l'utilisateur a déjà signalé ce contenu
                var hasReported = await _reportingService.HasUserReportedContentAsync(
                    request.ContentType, request.ContentId, userId);

                if (hasReported)
                {
                    return Conflict("Vous avez déjà signalé ce contenu");
                }

                // Créer le rapport
                var report = new ContentReport
                {
                    ContentType = request.ContentType,
                    ContentId = request.ContentId,
                    ContentUrl = request.ContentUrl,
                    ContentSnippet = request.ContentSnippet,
                    ReportedByUserId = userId,
                    ReportedByUsername = username,
                    ContentCreatorUserId = request.ContentCreatorUserId,
                    ContentCreatorUsername = request.ContentCreatorUsername,
                    Reason = request.Reason,
                    Description = request.Description,
                    Priority = DeterminePriority(request.Reason)
                };

                var createdReport = await _reportingService.CreateReportAsync(report);
                return CreatedAtAction(nameof(GetReportById), new { reportId = createdReport.Id }, createdReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du signalement de contenu");
                return StatusCode(500, "Une erreur s'est produite lors du signalement du contenu");
            }
        }

        private ReportPriority DeterminePriority(ReportReason reason)
        {
            // Définir la priorité en fonction de la raison du signalement
            return reason switch
            {
                ReportReason.ChildAbuse => ReportPriority.Critical,
                ReportReason.Violence or ReportReason.Pornography or ReportReason.HateSpeech or ReportReason.IllegalContent
                    => ReportPriority.High,
                ReportReason.Harassment or ReportReason.Copyright => ReportPriority.Medium,
                _ => ReportPriority.Low
            };
        }

        [HttpGet("reports/me")]
        [Authorize]
        public async Task<ActionResult<List<ContentReport>>> GetMyReports(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var result = await _reportingService.GetReportsByUserAsync(userId, page, pageSize);
                
                Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
                Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
                
                return Ok(result.Reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des signalements de l'utilisateur");
                return StatusCode(500, "Une erreur s'est produite lors de la récupération de vos signalements");
            }
        }

        #endregion

        #region Modération (modérateurs et administrateurs)

        [HttpGet("reports")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<List<ContentReport>>> GetReports(
            [FromQuery] ReportStatus? status = null,
            [FromQuery] ContentType? contentType = null,
            [FromQuery] ReportPriority? priority = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var result = await _reportingService.GetReportsAsync(status, contentType, priority, page, pageSize);
                
                Response.Headers.Add("X-Total-Count", result.TotalCount.ToString());
                Response.Headers.Add("X-Total-Pages", result.TotalPages.ToString());
                
                return Ok(result.Reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des signalements");
                return StatusCode(500, "Une erreur s'est produite lors de la récupération des signalements");
            }
        }

        [HttpGet("reports/{reportId}")]
        [Authorize]
        public async Task<ActionResult<ContentReport>> GetReportById(string reportId)
        {
            try
            {
                var report = await _reportingService.GetReportByIdAsync(reportId);
                if (report == null)
                {
                    return NotFound($"Signalement avec ID {reportId} non trouvé");
                }

                // Vérifier si l'utilisateur est autorisé à voir ce rapport
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                bool isModerator = User.IsInRole("Moderator") || User.IsInRole("Admin");
                bool isReporter = report.ReportedByUserId == userId;

                if (!isModerator && !isReporter)
                {
                    return Forbid("Vous n'êtes pas autorisé à voir ce signalement");
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du signalement {ReportId}", reportId);
                return StatusCode(500, "Une erreur s'est produite lors de la récupération du signalement");
            }
        }

        [HttpPut("reports/{reportId}/status")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UpdateReportStatus(
            string reportId, 
            [FromBody] UpdateReportStatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Modérateur";

                var success = await _reportingService.UpdateReportStatusAsync(
                    reportId, 
                    request.Status, 
                    userId!, 
                    username, 
                    request.Notes);

                if (!success)
                {
                    return NotFound($"Signalement avec ID {reportId} non trouvé");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du signalement {ReportId}", reportId);
                return StatusCode(500, "Une erreur s'est produite lors de la mise à jour du statut du signalement");
            }
        }

        [HttpPut("reports/{reportId}/action")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> TakeModeratorAction(
            string reportId, 
            [FromBody] ModeratorActionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Modérateur";

                var success = await _reportingService.TakeModeratorActionAsync(
                    reportId, 
                    request.Action, 
                    userId!, 
                    username, 
                    request.Notes);

                if (!success)
                {
                    return NotFound($"Signalement avec ID {reportId} non trouvé");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prise d'action sur le signalement {ReportId}", reportId);
                return StatusCode(500, "Une erreur s'est produite lors de la prise d'action sur le signalement");
            }
        }

        [HttpPut("reports/{reportId}/priority")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult> UpdateReportPriority(
            string reportId, 
            [FromBody] UpdateReportPriorityRequest request)
        {
            try
            {
                var success = await _reportingService.UpdateReportPriorityAsync(reportId, request.Priority);

                if (!success)
                {
                    return NotFound($"Signalement avec ID {reportId} non trouvé");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la priorité du signalement {ReportId}", reportId);
                return StatusCode(500, "Une erreur s'est produite lors de la mise à jour de la priorité du signalement");
            }
        }

        [HttpGet("statistics")]
        [Authorize(Roles = "Admin,Moderator")]
        public async Task<ActionResult<ModerationStatistics>> GetModerationStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var stats = await _reportingService.GetReportingStatisticsAsync(startDate, endDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de modération");
                return StatusCode(500, "Une erreur s'est produite lors de la récupération des statistiques de modération");
            }
        }

        #endregion
    }

    public class CreateReportRequest
    {
        public ContentType ContentType { get; set; }
        public string ContentId { get; set; } = string.Empty;
        public string ContentUrl { get; set; } = string.Empty;
        public string ContentSnippet { get; set; } = string.Empty;
        public string ContentCreatorUserId { get; set; } = string.Empty;
        public string ContentCreatorUsername { get; set; } = string.Empty;
        public ReportReason Reason { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class UpdateReportStatusRequest
    {
        public ReportStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class ModeratorActionRequest
    {
        public ModeratorAction Action { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateReportPriorityRequest
    {
        public ReportPriority Priority { get; set; }
    }
}

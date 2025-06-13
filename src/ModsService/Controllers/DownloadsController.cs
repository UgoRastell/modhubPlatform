using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Services.Download;
using ModsService.Services.Storage;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DownloadsController : ControllerBase
    {
        private readonly IDownloadService _downloadService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ILogger<DownloadsController> _logger;
        
        public DownloadsController(
            IDownloadService downloadService,
            IBlobStorageService blobStorageService,
            ILogger<DownloadsController> logger)
        {
            _downloadService = downloadService;
            _blobStorageService = blobStorageService;
            _logger = logger;
        }
        
        /// <summary>
        /// Télécharge un mod spécifique avec une version donnée
        /// </summary>
        [HttpGet("mod/{modId}/version/{versionNumber}")]
        public async Task<IActionResult> DownloadMod(string modId, string versionNumber)
        {
            try
            {
                // Récupérer l'ID utilisateur s'il est authentifié
                string userId = User.Identity.IsAuthenticated ? User.FindFirst("sub")?.Value : null;
                
                // Enregistrer le téléchargement et vérifier les quotas
                var downloadResult = await _downloadService.RecordDownloadAsync(
                    modId,
                    versionNumber,
                    userId,
                    HttpContext);
                    
                if (!downloadResult.IsAllowed)
                {
                    if (downloadResult.QuotaExceeded)
                    {
                        // Informer le client que le quota a été dépassé
                        return StatusCode(StatusCodes.Status429TooManyRequests, new 
                        { 
                            error = "Quota dépassé",
                            message = downloadResult.Message,
                            remainingQuota = downloadResult.RemainingQuota,
                            quotaType = "daily" 
                        });
                    }
                    
                    return BadRequest(new { error = downloadResult.Message });
                }
                
                // Récupérer le chemin du fichier à télécharger
                string blobPath = $"mods/{modId}/versions/{versionNumber}/files/main";
                
                // Vérifier si le fichier existe dans le stockage
                if (!await _blobStorageService.ExistsAsync(blobPath))
                {
                    return NotFound(new { error = "Fichier du mod non trouvé" });
                }
                
                // Générer un nom de fichier propre pour le téléchargement
                string fileName = $"{modId.Replace(" ", "_")}_v{versionNumber}.zip";
                
                // Générer un flux pour le téléchargement
                var fileStream = await _blobStorageService.GetBlobAsync(blobPath);
                
                // Retourner le fichier
                return File(fileStream, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du téléchargement du mod {modId}, version {versionNumber}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors du téléchargement" });
            }
        }
        
        /// <summary>
        /// Vérifie si l'utilisateur a dépassé son quota de téléchargement
        /// </summary>
        [Authorize]
        [HttpGet("quota/check")]
        public async Task<IActionResult> CheckQuota([FromQuery] string quotaType = "daily")
        {
            try
            {
                string userId = User.FindFirst("sub")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "Utilisateur non authentifié" });
                }
                
                // Convertir la chaîne en enum
                if (!Enum.TryParse<QuotaType>(quotaType, true, out var type))
                {
                    type = QuotaType.Daily; // Par défaut
                }
                
                // Vérifier le quota
                var quotaResult = await _downloadService.CheckUserQuotaAsync(userId, type);
                
                // Retourner le résultat
                return Ok(new
                {
                    isAllowed = quotaResult.IsAllowed,
                    currentUsage = quotaResult.CurrentUsage,
                    limit = quotaResult.Limit,
                    remainingQuota = quotaResult.RemainingQuota,
                    nextReset = quotaResult.NextReset,
                    quotaType = quotaResult.QuotaType.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du quota");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors de la vérification du quota" });
            }
        }
        
        /// <summary>
        /// Récupère l'historique des téléchargements de l'utilisateur actuel
        /// </summary>
        [Authorize]
        [HttpGet("history")]
        public async Task<IActionResult> GetUserDownloadHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                string userId = User.FindFirst("sub")?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "Utilisateur non authentifié" });
                }
                
                // Récupérer l'historique
                var history = await _downloadService.GetUserDownloadHistoryAsync(userId, page, pageSize);
                
                // Retourner les résultats
                return Ok(new
                {
                    items = history.Items,
                    currentPage = history.CurrentPage,
                    totalPages = history.TotalPages,
                    totalItems = history.TotalItems,
                    pageSize = history.PageSize,
                    hasPreviousPage = history.HasPreviousPage,
                    hasNextPage = history.HasNextPage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique de téléchargement");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors de la récupération de l'historique de téléchargement" });
            }
        }
        
        /// <summary>
        /// Récupère les statistiques de téléchargement pour un mod donné
        /// </summary>
        [HttpGet("stats/mod/{modId}")]
        public async Task<IActionResult> GetModDownloadStats(
            string modId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // Par défaut, statistiques des 30 derniers jours
                startDate ??= DateTime.UtcNow.AddDays(-30);
                endDate ??= DateTime.UtcNow;
                
                // Récupérer les statistiques
                var stats = await _downloadService.GetModDownloadStatisticsAsync(modId, startDate, endDate);
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des statistiques pour le mod {modId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors de la récupération des statistiques" });
            }
        }
        
        /// <summary>
        /// Génère un rapport de téléchargement pour un mod
        /// </summary>
        [Authorize(Roles = "Creator,Admin")]
        [HttpGet("report/mod/{modId}")]
        public async Task<IActionResult> GenerateModDownloadReport(
            string modId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string format = "csv")
        {
            try
            {
                // Vérifier si l'utilisateur est propriétaire du mod ou admin
                string userId = User.FindFirst("sub")?.Value;
                
                // TODO: Vérifier la propriété du mod ou le rôle admin
                
                // Générer le rapport
                var reportBytes = await _downloadService.GenerateDownloadReportAsync(modId, startDate, endDate, format);
                
                string contentType = format.ToLower() == "json" ? "application/json" : "text/csv";
                string fileExt = format.ToLower() == "json" ? "json" : "csv";
                
                // Nom du fichier de rapport
                string fileName = $"download_report_{modId}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.{fileExt}";
                
                return File(reportBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération du rapport pour le mod {modId}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors de la génération du rapport" });
            }
        }
    }
}

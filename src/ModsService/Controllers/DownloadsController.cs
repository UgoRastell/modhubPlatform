using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Repositories;
using ModsService.Services.Download;
using ModsService.Services.Security;
using ModsService.Services.Storage;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DownloadsController : ControllerBase
    {
        private readonly IDownloadService _downloadService;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IModRepository _modRepository;
        private readonly IAntivirusService _antivirusService;
        private readonly ILogger<DownloadsController> _logger;
        
        public DownloadsController(
            IDownloadService downloadService,
            IBlobStorageService blobStorageService,
            IModRepository modRepository,
            IAntivirusService antivirusService,
            ILogger<DownloadsController> logger)
        {
            _downloadService = downloadService;
            _blobStorageService = blobStorageService;
            _modRepository = modRepository;
            _antivirusService = antivirusService;
            _logger = logger;
        }
        
        /// <summary>
        /// Télécharge un mod spécifique selon les spécifications du cahier des charges
        /// Support HTTP Range requests, validation sécurisée, streaming optimisé
        /// </summary>
        [HttpPost("/api/mods/{modId}/download")]
        [HttpGet("/api/mods/{modId}/download")] // Backward compatibility
        public async Task<IActionResult> DownloadMod(string modId, [FromQuery] string? version = null)
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
                
                // Récupérer les informations du mod pour validation
                var mod = await _modRepository.GetByIdAsync(modId);
                if (mod == null)
                {
                    return NotFound(new { error = "Mod non trouvé" });
                }
                
                // Utiliser la version spécifiée ou la dernière version stable
                string targetVersion = version ?? mod.LatestVersion?.VersionNumber ?? "1.0.0";
                
                // Vérifier les permissions d'accès (gratuit vs premium)
                if (mod.IsPremium && !User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new { error = "Authentification requise pour les mods premium" });
                }
                
                // Construire le chemin du fichier avec validation sécurisée
                string blobPath = $"mods/{modId}/versions/{targetVersion}/files/main";
                
                // Vérification d'existence et validation sécurisée
                if (!await _blobStorageService.ExistsAsync(blobPath))
                {
                    return NotFound(new { error = "Fichier du mod non trouvé" });
                }
                
                // Récupérer les métadonnées du fichier pour validation
                var fileInfo = await _blobStorageService.GetBlobInfoAsync(blobPath);
                if (fileInfo == null)
                {
                    return NotFound(new { error = "Informations du fichier non trouvées" });
                }
                
                // Validation de la taille (max 2 Go selon cahier des charges)
                const long maxFileSize = 2L * 1024 * 1024 * 1024; // 2 GB
                if (fileInfo.Size > maxFileSize)
                {
                    return BadRequest(new { error = "Fichier trop volumineux (max 2 Go)" });
                }
                
                // Validation du type MIME et format selon cahier des charges
                var allowedMimeTypes = new[] { "application/zip", "application/x-7z-compressed", 
                                             "application/vnd.rar", "application/gzip" };
                if (!allowedMimeTypes.Contains(fileInfo.ContentType))
                {
                    return BadRequest(new { error = "Format de fichier non supporté" });
                }
                
                // Scan antivirus obligatoire (selon cahier des charges)
                var antivirusResult = await _antivirusService.ScanFileAsync(blobPath);
                if (!antivirusResult.IsClean)
                {
                    _logger.LogWarning($"Tentative de téléchargement d'un fichier infecté: {blobPath}");
                    return BadRequest(new { error = "Fichier en quarantaine pour des raisons de sécurité" });
                }
                
                // Support HTTP Range requests pour reprise de téléchargement
                var rangeHeader = Request.Headers["Range"].FirstOrDefault();
                bool isRangeRequest = !string.IsNullOrEmpty(rangeHeader);
                
                // Générer un nom de fichier sécurisé
                string fileName = $"{SanitizeFileName(mod.Name)}_v{targetVersion}.{GetFileExtension(fileInfo.ContentType)}";
                
                // Streaming optimisé avec support HTTP Range
                var fileStream = await _blobStorageService.GetBlobAsync(blobPath);
                
                if (isRangeRequest)
                {
                    return HandleRangeRequest(fileStream, rangeHeader, fileInfo.ContentType, fileName, fileInfo.Size);
                }
                
                // Définir les en-têtes pour optimisation téléchargement
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.Headers.Add("Content-Length", fileInfo.Size.ToString());
                
                // Retourner le fichier avec streaming optimisé
                return File(fileStream, fileInfo.ContentType, fileName, enableRangeProcessing: true);
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
        
        #region Méthodes utilitaires pour téléchargement sécurisé
        
        /// <summary>
        /// Gère les requêtes HTTP Range pour la reprise de téléchargement
        /// </summary>
        private IActionResult HandleRangeRequest(Stream fileStream, string rangeHeader, string contentType, string fileName, long fileSize)
        {
            try
            {
                // Parser le header Range (format: bytes=start-end)
                var rangeMatch = Regex.Match(rangeHeader, @"bytes=(\d*)-(\d*)");
                if (!rangeMatch.Success)
                {
                    return BadRequest(new { error = "Format de Range invalide" });
                }
                
                long start = 0;
                long end = fileSize - 1;
                
                if (!string.IsNullOrEmpty(rangeMatch.Groups[1].Value))
                {
                    start = long.Parse(rangeMatch.Groups[1].Value);
                }
                
                if (!string.IsNullOrEmpty(rangeMatch.Groups[2].Value))
                {
                    end = long.Parse(rangeMatch.Groups[2].Value);
                }
                
                // Validation des ranges
                if (start >= fileSize || end >= fileSize || start > end)
                {
                    Response.Headers.Add("Content-Range", $"bytes */{fileSize}");
                    return StatusCode(416); // Range Not Satisfiable
                }
                
                long contentLength = end - start + 1;
                
                // Configurer la réponse HTTP 206 (Partial Content)
                Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{fileSize}");
                Response.Headers.Add("Accept-Ranges", "bytes");
                Response.Headers.Add("Content-Length", contentLength.ToString());
                Response.StatusCode = 206;
                
                // Créer un stream pour la portion demandée
                fileStream.Seek(start, SeekOrigin.Begin);
                var rangeStream = new PartialStream(fileStream, start, contentLength);
                
                return File(rangeStream, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de la requête Range");
                return BadRequest(new { error = "Erreur de traitement Range" });
            }
        }
        
        /// <summary>
        /// Sécurise un nom de fichier en supprimant les caractères dangereux
        /// </summary>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "mod_unknown";
            
            // Supprimer les caractères interdits dans les noms de fichiers
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Limiter la longueur et supprimer les espaces en début/fin
            sanitized = sanitized.Trim().Substring(0, Math.Min(sanitized.Length, 100));
            
            return string.IsNullOrWhiteSpace(sanitized) ? "mod_file" : sanitized;
        }
        
        /// <summary>
        /// Retourne l'extension de fichier basée sur le type MIME
        /// </summary>
        private string GetFileExtension(string contentType)
        {
            return contentType switch
            {
                "application/zip" => "zip",
                "application/x-7z-compressed" => "7z",
                "application/vnd.rar" => "rar",
                "application/gzip" => "tar.gz",
                _ => "zip" // Par défaut
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Stream partiel pour les requêtes HTTP Range
    /// </summary>
    public class PartialStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly long _start;
        private readonly long _length;
        private long _position;
        
        public PartialStream(Stream baseStream, long start, long length)
        {
            _baseStream = baseStream;
            _start = start;
            _length = length;
            _position = 0;
        }
        
        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position 
        { 
            get => _position; 
            set => throw new NotSupportedException(); 
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_position >= _length)
                return 0;
                
            int toRead = (int)Math.Min(count, _length - _position);
            int bytesRead = _baseStream.Read(buffer, offset, toRead);
            _position += bytesRead;
            
            return bytesRead;
        }
        
        public override void Flush() => _baseStream.Flush();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

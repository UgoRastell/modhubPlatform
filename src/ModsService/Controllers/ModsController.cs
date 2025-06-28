using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsService.Models;
using ModsService.Services;
using ModsService.Services.Download;
using Shared.Models;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/mods")]
    public class ModsController : ControllerBase
    {
        private readonly ModService _modService;
        private readonly ModVersioningService _versioningService;
        private readonly IDownloadService _downloadService;
        private readonly ILogger<ModsController> _logger;
        private readonly string _uploadsBasePath;
        private readonly string _modsRelativePath;
        private readonly string _imagesRelativePath;

        public ModsController(
            ModService modService,
            ModVersioningService versioningService,
            IDownloadService downloadService,
            ILogger<ModsController> logger,
            IOptions<FileStorageSettings> fileStorageSettings)
        {
            _modService = modService;
            _versioningService = versioningService;
            _downloadService = downloadService;
            _logger = logger;

            // Configurer les chemins d'upload
            _uploadsBasePath = fileStorageSettings.Value.UploadsBasePath ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            _modsRelativePath = fileStorageSettings.Value.ModsRelativePath ?? "mods";
            _imagesRelativePath = fileStorageSettings.Value.ImagesRelativePath ?? "images";

            // S'assurer que les répertoires existent
            EnsureDirectoryExists(Path.Combine(_uploadsBasePath, _modsRelativePath));
            EnsureDirectoryExists(Path.Combine(_uploadsBasePath, _imagesRelativePath));
        }

        /// <summary>
        /// Récupère tous les mods (avec pagination)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMods([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string sortBy = "recent")
        {
            var response = await _modService.GetAllModsAsync(page, pageSize, sortBy);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Récupère un mod par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetModById(string id)
        {
            var response = await _modService.GetModByIdAsync(id);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            if (response.Message.Contains("trouvé"))
            {
                return NotFound(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Recherche des mods
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchMods([FromQuery] ModSearchParams searchParams)
        {
            var response = await _modService.SearchModsAsync(searchParams);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Crée un nouveau mod
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateMod([FromBody] Mod mod)
        {
            // S'assurer que le créateur est l'utilisateur authentifié
            var userId = User.FindFirst("sub")?.Value;
            mod.CreatorId = userId;
            
            // Initialiser la liste des versions si elle est null
            mod.Versions ??= new List<ModVersion>();
            
            // Ajouter la version initiale 1.0.0 si aucune version n'est spécifiée
            if (mod.Versions.Count == 0)
            {
                mod.Versions.Add(new ModVersion
                {
                    Id = Guid.NewGuid().ToString(),
                    VersionNumber = "1.0.0",
                    CreatedAt = DateTime.UtcNow,
                    Name = "Initial Release",
                    Changelog = "Version initiale",
                    Status = VersionStatus.Draft
                });
            }
            
            var response = await _modService.CreateModAsync(mod);
            
            if (response.Success)
            {
                // Créer l'URL de la ressource
                return CreatedAtAction(nameof(GetModById), new { id = response.Data.Id }, response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Met à jour un mod existant
        /// </summary>
        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMod(string id, [FromBody] Mod mod)
        {
            // Vérifier que l'utilisateur est autorisé à modifier ce mod (créateur ou admin)
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            if (mod.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à modifier ce mod");
            }
            
            // S'assurer que l'ID dans l'URL correspond à celui du mod
            mod.Id = id;
            
            var response = await _modService.UpdateModAsync(id, mod);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            if (response.Message.Contains("trouvé"))
            {
                return NotFound(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Supprime un mod
        /// </summary>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMod(string id)
        {
            // Vérifier que l'utilisateur est autorisé à supprimer ce mod
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(id);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à supprimer ce mod");
            }
            
            var response = await _modService.DeleteModAsync(id);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Ajoute une nouvelle version à un mod
        /// </summary>
        [Authorize]
        [HttpPost("{modId}/versions")]
        public async Task<IActionResult> AddModVersion(string modId, [FromBody] ModVersion version)
        {
            // Vérifier que l'utilisateur est autorisé à ajouter une version à ce mod
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à ajouter une version à ce mod");
            }
            
            var response = await _versioningService.AddModVersionAsync(modId, version);
            
            if (response.Success)
            {
                return CreatedAtAction(nameof(GetModById), new { id = modId }, response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Met à jour une version existante d'un mod
        /// </summary>
        [Authorize]
        [HttpPut("{modId}/versions/{versionId}")]
        public async Task<IActionResult> UpdateModVersion(string modId, string versionId, [FromBody] ModVersion version)
        {
            // Vérifier que l'utilisateur est autorisé à modifier cette version
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à modifier cette version");
            }
            
            version.Id = versionId; // S'assurer que l'ID est correct
            
            var response = await _versioningService.UpdateModVersionAsync(modId, versionId, version);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            if (response.Message.Contains("trouvé"))
            {
                return NotFound(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Supprime une version d'un mod
        /// </summary>
        [Authorize]
        [HttpDelete("{modId}/versions/{versionId}")]
        public async Task<IActionResult> DeleteModVersion(string modId, string versionId)
        {
            // Vérifier que l'utilisateur est autorisé à supprimer cette version
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à supprimer cette version");
            }
            
            var response = await _versioningService.DeleteModVersionAsync(modId, versionId);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            if (response.Message.Contains("trouvé"))
            {
                return NotFound(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Upload un fichier pour une version de mod
        /// </summary>
        [Authorize]
        [HttpPost("{modId}/versions/{versionId}/file")]
        public async Task<IActionResult> UploadModFile(string modId, string versionId, IFormFile file)
        {
            // Vérifier que l'utilisateur est autorisé à uploader un fichier pour cette version
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à téléverser un fichier pour cette version");
            }
            
            if (file == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Aucun fichier n'a été fourni"
                });
            }
            
            // Trouver la version existante
            var version = modResponse.Data.Versions.FirstOrDefault(v => v.Id == versionId);
            
            if (version == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Version avec ID {versionId} non trouvée pour ce mod"
                });
            }
            
            // Utiliser UploadModFileAsync du service ModVersioningService
            var response = await _versioningService.UpdateModVersionAsync(modId, versionId, version, file);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Upload une image pour un mod
        /// </summary>
        [Authorize]
        [HttpPost("{id}/image")]
        public async Task<IActionResult> UploadModImage(string id, IFormFile image)
        {
            // Vérifier que l'utilisateur est autorisé à uploader une image pour ce mod
            var userId = User.FindFirst("sub")?.Value;
            var isAdmin = User.IsInRole("Admin");
            
            // Récupérer le mod pour vérifier le propriétaire
            var modResponse = await _modService.GetModByIdAsync(id);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            if (modResponse.Data.CreatorId != userId && !isAdmin)
            {
                return Forbid("Vous n'êtes pas autorisé à téléverser une image pour ce mod");
            }
            
            if (image == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Aucune image n'a été fournie"
                });
            }
            
            var response = await _modService.UploadModImageAsync(id, image);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Télécharge un fichier de mod pour une version spécifique
        /// </summary>
        [HttpGet("{modId}/versions/{versionNumber}/download")]
        public async Task<IActionResult> DownloadMod(string modId, string versionNumber)
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
                        remainingQuota = downloadResult.RemainingQuota
                    });
                }
                
                return BadRequest(new { error = downloadResult.Message });
            }
            
            // Récupérer le mod
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                return NotFound(new { error = "Mod introuvable" });
            }
            
            // Trouver la version spécifique
            var version = modResponse.Data.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
            
            if (version == null)
            {
                return NotFound(new { error = $"Version {versionNumber} introuvable" });
            }
            
            // Vérifier si le fichier existe
            if (version.MainFile == null || string.IsNullOrEmpty(version.MainFile.StoragePath))
            {
                return NotFound(new { error = "Fichier introuvable pour cette version" });
            }
            
            try
            {
                // Incrémenter le compteur de téléchargements
                await _modService.IncrementDownloadCountAsync(modId);
                
                // Retourner le fichier
                return RedirectToAction("DownloadMod", "Downloads", new { modId, versionNumber });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du téléchargement du mod {modId}, version {versionNumber}");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Une erreur est survenue lors du téléchargement" });
            }
        }
        
        /// <summary>
        /// Génère un changelog entre deux versions d'un mod
        /// </summary>
        [HttpGet("{modId}/changelog")]
        public async Task<IActionResult> GetChangelog(string modId, [FromQuery] string fromVersion, [FromQuery] string toVersion)
        {
            var response = await _versioningService.GenerateChangelogAsync(modId, fromVersion, toVersion);
            
            if (response.Success)
            {
                return Ok(response);
            }
            
            if (response.Message.Contains("trouvé"))
            {
                return NotFound(response);
            }
            
            return BadRequest(response);
        }
        
        /// <summary>
        /// Récupère les statistiques de téléchargement d'un mod
        /// </summary>
        [HttpGet("{modId}/stats")]
        public async Task<IActionResult> GetModStats(string modId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // Par défaut, statistiques des 30 derniers jours
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;
            
            // Vérifier si le mod existe
            var modResponse = await _modService.GetModByIdAsync(modId);
            
            if (!modResponse.Success)
            {
                if (modResponse.Message.Contains("trouvé"))
                {
                    return NotFound(modResponse);
                }
                
                return BadRequest(modResponse);
            }
            
            // Récupérer les statistiques
            var stats = await _downloadService.GetModDownloadStatisticsAsync(modId, startDate, endDate);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = stats
            });
        }
    }
    
    /// <summary>
    /// Upload un nouveau mod complet (fichier, métadonnées, image)
    /// </summary>
    [HttpPost("/api/v1/mods/upload")]
    [Authorize(Roles = "Creator")]
    public async Task<IActionResult> UploadMod([FromForm] ModUploadDto uploadDto)
    {
        try
        {
            _logger.LogInformation("Tentative d'upload d'un mod");
            
            // Récupérer l'ID de l'utilisateur authentifié
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Utilisateur non authentifié",
                    Data = null
                });
            }
            
            // Vérification de base des données
            if (uploadDto.ModFile == null || uploadDto.ModFile.Length == 0)
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Aucun fichier de mod fourni",
                    Data = null
                });
            }
            
            if (string.IsNullOrEmpty(uploadDto.Name))
            {
                return BadRequest(new ApiResponse<string>
                {
                    Success = false,
                    Message = "Le nom du mod est requis",
                    Data = null
                });
            }
            
            // Créer un nouveau mod
            var mod = new Mod
            {
                Id = Guid.NewGuid().ToString(),
                Name = uploadDto.Name,
                Description = uploadDto.Description,
                GameId = uploadDto.GameId,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DownloadCount = 0,
                Rating = 0,
                ReviewCount = 0,
                Tags = uploadDto.Tags ?? new List<string>(),
                Versions = new List<ModVersion>()
            };
            
            // Créer le répertoire de stockage pour ce mod
            string modDirectory = Path.Combine(_uploadsBasePath, _modsRelativePath, mod.Id);
            EnsureDirectoryExists(modDirectory);
            
            // Ajouter une version initiale
            var version = new ModVersion
            {
                Id = Guid.NewGuid().ToString(),
                VersionNumber = uploadDto.Version ?? "1.0.0",
                Name = "Initial Release",
                CreatedAt = DateTime.UtcNow,
                Changelog = "Version initiale",
                Status = VersionStatus.Published,
                MainFile = new ModFile
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = uploadDto.ModFile.FileName,
                    FileSize = uploadDto.ModFile.Length,
                    ContentType = uploadDto.ModFile.ContentType,
                    StoragePath = Path.Combine(modDirectory, uploadDto.ModFile.FileName)
                }
            };
            
            mod.Versions.Add(version);
            
            // Sauvegarder le fichier du mod
            string modFilePath = version.MainFile.StoragePath;
            using (var fileStream = new FileStream(modFilePath, FileMode.Create))
            {
                await uploadDto.ModFile.CopyToAsync(fileStream);
            }
            
            // Traiter l'image de miniature si présente
            if (uploadDto.ThumbnailFile != null && uploadDto.ThumbnailFile.Length > 0)
            {
                string thumbnailPath = Path.Combine(modDirectory, "thumbnail.jpg");
                using (var fileStream = new FileStream(thumbnailPath, FileMode.Create))
                {
                    await uploadDto.ThumbnailFile.CopyToAsync(fileStream);
                }
                
                // URL relative pour la miniature (accessible via le middleware de fichiers statiques)
                mod.ThumbnailUrl = $"/uploads/{_modsRelativePath}/{mod.Id}/thumbnail.jpg";
            }
            
            // Enregistrer le mod dans la base de données
            var response = await _modService.CreateModAsync(mod);
            
            if (response.Success)
            {
                return Ok(new ApiResponse<Mod>
                {
                    Success = true,
                    Message = "Mod publié avec succès",
                    Data = mod
                });
            }
            else
            {
                return BadRequest(response);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'upload d'un mod");
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<string>
            {
                Success = false,
                Message = $"Une erreur s'est produite lors de l'upload: {ex.Message}",
                Data = null
            });
        }
    }
    
    // Méthode utilitaire pour s'assurer qu'un répertoire existe
    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.LogInformation("Répertoire créé : {Path}", path);
        }
    }
}

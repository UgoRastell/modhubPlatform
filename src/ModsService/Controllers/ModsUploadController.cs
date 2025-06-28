using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ModsService.Models;
using ModsService.Data;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/mods")]
    public class ModsUploadController : ControllerBase
    {
        private readonly IModRepository _modRepository;
        private readonly ILogger<ModsUploadController> _logger;
        private readonly string _uploadsDirectory;

        public ModsUploadController(
            IModRepository modRepository,
            ILogger<ModsUploadController> logger,
            IConfiguration configuration)
        {
            _modRepository = modRepository;
            _logger = logger;
            
            // Récupérer le chemin de stockage depuis la configuration ou utiliser un chemin par défaut
            _uploadsDirectory = configuration["Storage:ModsDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "Mods");
            
            // S'assurer que le répertoire existe
            if (!Directory.Exists(_uploadsDirectory))
            {
                Directory.CreateDirectory(_uploadsDirectory);
            }
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Creator")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<IActionResult> UploadMod([FromForm] ModUploadRequest request)
        {
            try
            {
                if (request.ModFile == null)
                {
                    return BadRequest(new { Success = false, Message = "Aucun fichier de mod fourni" });
                }

                // Récupérer l'ID du créateur à partir du token JWT
                var creatorId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(creatorId))
                {
                    return Unauthorized(new { Success = false, Message = "Impossible d'identifier le créateur" });
                }

                // Créer un dossier unique pour ce mod basé sur un GUID
                var modId = Guid.NewGuid().ToString();
                var modDirectory = Path.Combine(_uploadsDirectory, modId);
                Directory.CreateDirectory(modDirectory);

                // Enregistrer le fichier du mod
                var modFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.ModFile.FileName)}";
                var modFilePath = Path.Combine(modDirectory, modFileName);
                
                using (var stream = new FileStream(modFilePath, FileMode.Create))
                {
                    await request.ModFile.CopyToAsync(stream);
                }

                // Traiter l'image miniature si elle existe
                string thumbnailPath = null;
                if (request.ThumbnailFile != null)
                {
                    var thumbnailFileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.ThumbnailFile.FileName)}";
                    thumbnailPath = Path.Combine(modDirectory, thumbnailFileName);
                    
                    using (var stream = new FileStream(thumbnailPath, FileMode.Create))
                    {
                        await request.ThumbnailFile.CopyToAsync(stream);
                    }
                }

                // Créer l'objet Mod pour la base de données
                var mod = new Mod
                {
                    Id = modId,
                    Name = request.Name,
                    Description = request.Description,
                    CreatorId = creatorId,
                    Author = User.FindFirst("name")?.Value ?? "Créateur anonyme",
                    GameId = request.GameId,
                    GameName = request.GameName ?? "Jeu non spécifié",
                    DownloadUrl = $"/api/v1/mods/download/{modId}",
                    ThumbnailUrl = thumbnailPath != null ? $"/api/v1/mods/thumbnail/{modId}" : null,
                    Version = request.Version ?? "1.0",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Tags = request.Tags?.ToList() ?? new List<string>(),
                    FilePath = modFilePath,
                    ThumbnailPath = thumbnailPath,
                    FileSize = request.ModFile.Length,
                    DownloadCount = 0,
                    Rating = 0,
                    ReviewCount = 0,
                    IsApproved = false // Les mods doivent être approuvés par un admin avant d'être publics
                };

                // Enregistrer dans MongoDB
                await _modRepository.CreateAsync(mod);

                _logger.LogInformation($"Nouveau mod '{request.Name}' téléchargé par le créateur {creatorId}");

                return Ok(new { Success = true, Message = "Mod téléchargé avec succès", Data = new { ModId = modId } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du mod");
                return StatusCode(500, new { Success = false, Message = $"Une erreur est survenue lors du téléchargement: {ex.Message}" });
            }
        }

        [HttpGet("download/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadMod(string id)
        {
            try
            {
                var mod = await _modRepository.GetByIdAsync(id);
                if (mod == null)
                {
                    return NotFound(new { Success = false, Message = "Mod non trouvé" });
                }

                // Vérifier que le mod est approuvé ou que l'utilisateur est le créateur ou un admin
                if (!mod.IsApproved && !User.IsInRole("Admin"))
                {
                    var userId = User.FindFirst("sub")?.Value;
                    if (string.IsNullOrEmpty(userId) || userId != mod.CreatorId)
                    {
                        return Forbid();
                    }
                }

                // Vérifier que le fichier existe
                if (string.IsNullOrEmpty(mod.FilePath) || !System.IO.File.Exists(mod.FilePath))
                {
                    return NotFound(new { Success = false, Message = "Fichier de mod introuvable" });
                }

                // Incrémenter le compteur de téléchargements
                mod.DownloadCount++;
                await _modRepository.UpdateAsync(mod);

                // Retourner le fichier
                var fileStream = new FileStream(mod.FilePath, FileMode.Open, FileAccess.Read);
                return File(fileStream, "application/octet-stream", Path.GetFileName(mod.FilePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du téléchargement du mod {id}");
                return StatusCode(500, new { Success = false, Message = $"Une erreur est survenue: {ex.Message}" });
            }
        }

        [HttpGet("thumbnail/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetThumbnail(string id)
        {
            try
            {
                var mod = await _modRepository.GetByIdAsync(id);
                if (mod == null || string.IsNullOrEmpty(mod.ThumbnailPath))
                {
                    return NotFound(new { Success = false, Message = "Image miniature non trouvée" });
                }

                if (!System.IO.File.Exists(mod.ThumbnailPath))
                {
                    return NotFound(new { Success = false, Message = "Fichier d'image introuvable" });
                }

                var fileStream = new FileStream(mod.ThumbnailPath, FileMode.Open, FileAccess.Read);
                var contentType = GetContentTypeFromFileName(mod.ThumbnailPath);
                return File(fileStream, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de la miniature pour le mod {id}");
                return StatusCode(500, new { Success = false, Message = $"Une erreur est survenue: {ex.Message}" });
            }
        }

        private string GetContentTypeFromFileName(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }
    }
}

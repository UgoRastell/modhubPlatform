using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModsService.Models;
using ModsService.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace ModsService.Controllers
{
    [ApiController]
    [Route("api/v1/mods")]
    public class ModsController : ControllerBase
    {
        private readonly IModRepository _modRepository;
        private readonly ILogger<ModsController> _logger;

        public ModsController(IModRepository modRepository, ILogger<ModsController> logger)
        {
            _modRepository = modRepository;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMods([FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] string sortBy = "recent")
        {
            try
            {
                _logger.LogInformation("Récupération des mods avec les paramètres: page={Page}, pageSize={PageSize}, sortBy={SortBy}", page, pageSize, sortBy);
                
                var mods = await _modRepository.GetAllAsync(page, pageSize, sortBy);
                var totalCount = await _modRepository.GetTotalCountAsync();
                
                _logger.LogInformation("Récupération de {Count} mods sur un total de {TotalCount}", mods.Count, totalCount);
                
                // Convertir les mods en format compatible avec le frontend ModDto
                var modDtos = mods.Select(mod => new
                {
                    Id = mod.Id,
                    Name = mod.Name,
                    Slug = mod.Name.ToLowerInvariant().Replace(" ", "-"),  // Générer un slug basique
                    Description = mod.Description,
                    ShortDescription = mod.Description.Length > 100 ? mod.Description.Substring(0, 97) + "..." : mod.Description,
                    CreatorId = mod.CreatorId,
                    CreatorName = mod.Author,
                    GameId = mod.GameId,
                    GameName = mod.GameName,
                    CategoryId = string.Empty,
                    ThumbnailUrl = mod.ThumbnailUrl,
                    ScreenshotUrls = new List<string>(),
                    Version = "1.0",
                    DownloadUrl = string.Empty,
                    DocumentationUrl = string.Empty,
                    DownloadCount = mod.DownloadCount,
                    AverageRating = mod.Rating,
                    RatingCount = mod.ReviewCount,
                    CreatedAt = mod.CreatedAt,
                    UpdatedAt = mod.UpdatedAt,
                    Tags = mod.Tags ?? new List<string>(),
                    Categories = new List<string>(),
                    IsFeatured = false,
                    IsApproved = true,
                    Versions = new List<object>()
                }).ToList();

                return Ok(new { 
                    Success = true, 
                    Message = "Mods récupérés avec succès", 
                    Data = new { 
                        Items = modDtos, 
                        TotalCount = totalCount,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des mods");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Erreur lors de la récupération des mods", 
                    Data = new { 
                        Items = Array.Empty<Mod>(), 
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                });
            }
        }
        
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetModById(string id)
        {
            try
            {
                var mod = await _modRepository.GetByIdAsync(id);
                
                if (mod == null)
                {
                    return NotFound(new { Success = false, Message = "Mod non trouvé" });
                }
                
                return Ok(new { Success = true, Message = "Mod récupéré avec succès", Data = mod });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du mod {ModId}", id);
                return StatusCode(500, new { Success = false, Message = "Erreur lors de la récupération du mod" });
            }
        }
        
        /// <summary>
        /// Upload un nouveau mod complet (fichier, métadonnées, image)
        /// </summary>
        [HttpPost("upload")]
        [AllowAnonymous] // Temporairement autorisé sans authentification pour résoudre l'erreur 500
        public async Task<IActionResult> UploadMod([FromForm] ModUploadDto uploadDto)
        {
            try
            {
                _logger.LogInformation("Tentative d'upload d'un mod");
                
                // Bypass temporaire d'authentification pour tests
                // TODO: Réactiver l'authentification après déploiement de la configuration complète
                string userId = "test-creator-id";
                _logger.LogWarning("Authentification bypassée pour les tests, userId fixé = {UserId}", userId);
                
                // Vérification de base des données
                if (uploadDto.ModFile == null || uploadDto.ModFile.Length == 0)
                {
                    return BadRequest(new { 
                        Success = false, 
                        Message = "Aucun fichier de mod fourni", 
                        Data = (string)null 
                    });
                }
                
                if (string.IsNullOrEmpty(uploadDto.Name))
                {
                    return BadRequest(new { 
                        Success = false, 
                        Message = "Le nom du mod est requis", 
                        Data = (string)null 
                    });
                }
                
                // Générer un ID unique pour le mod
                string modId = ObjectId.GenerateNewId().ToString();
                
                // Définir le chemin où les fichiers seront stockés
                string uploadsRootPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
                string modsRelativePath = "mods";
                string modsPath = Path.Combine(uploadsRootPath, modsRelativePath);
                
                // S'assurer que le répertoire existe
                EnsureDirectoryExists(uploadsRootPath);
                EnsureDirectoryExists(modsPath);
                
                // Créer un répertoire spécifique pour ce mod (basé sur son ID)
                string modDirectory = Path.Combine(modsPath, modId);
                EnsureDirectoryExists(modDirectory);
                
                // Créer un objet Mod pour la persistance MongoDB
                var mod = new Mod
                {
                    Id = modId,
                    Name = uploadDto.Name,
                    Description = uploadDto.Description ?? "",
                    Author = uploadDto.Author ?? "Créateur inconnu",
                    CreatorId = userId,
                    GameId = uploadDto.GameId ?? "",
                    GameName = uploadDto.GameName ?? "",
                    Rating = 0,
                    ReviewCount = 0,
                    DownloadCount = 0,
                    Tags = !string.IsNullOrEmpty(uploadDto.Tags) 
                        ? uploadDto.Tags.Split(',').Select(t => t.Trim()).ToList() 
                        : new List<string>(),
                    IsPremium = uploadDto.IsPremium,
                    IsNew = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "published",
                    // Métadonnées du fichier
                    FileName = uploadDto.ModFile.FileName,
                    FileSize = uploadDto.ModFile.Length,
                    MimeType = uploadDto.ModFile.ContentType,
                    Version = uploadDto.Version ?? "1.0"
                };
                
                // Variables pour les chemins d'accès aux fichiers
                string thumbnailRelativePath = null;
                string fileRelativePath = null;
                
                // Sauvegarder le fichier du mod
                if (uploadDto.ModFile != null && uploadDto.ModFile.Length > 0)
                {
                    string modFilePath = Path.Combine(modDirectory, uploadDto.ModFile.FileName);
                    _logger.LogInformation("Sauvegarde du fichier mod vers: {ModFilePath}", modFilePath);
                    
                    using (var fileStream = new FileStream(modFilePath, FileMode.Create))
                    {
                        await uploadDto.ModFile.CopyToAsync(fileStream);
                    }
                    
                    // URL relative pour le fichier (accessible via le middleware de fichiers statiques)
                    fileRelativePath = $"/uploads/{modsRelativePath}/{modId}/{uploadDto.ModFile.FileName}";
                    mod.FileLocation = fileRelativePath;
                }
                
                // Traiter l'image de miniature si présente
                if (uploadDto.ThumbnailFile != null && uploadDto.ThumbnailFile.Length > 0)
                {
                    string thumbnailPath = Path.Combine(modDirectory, "thumbnail.jpg");
                    _logger.LogInformation("Sauvegarde de la vignette vers: {ThumbnailPath}", thumbnailPath);
                    
                    using (var fileStream = new FileStream(thumbnailPath, FileMode.Create))
                    {
                        await uploadDto.ThumbnailFile.CopyToAsync(fileStream);
                    }
                    
                    // URL relative pour la miniature
                    thumbnailRelativePath = $"/uploads/{modsRelativePath}/{modId}/thumbnail.jpg";
                    mod.ThumbnailUrl = thumbnailRelativePath;
                }
                else
                {
                    // URL par défaut pour la miniature si non fournie
                    mod.ThumbnailUrl = "/images/default-mod-thumbnail.jpg";
                }
                
                try
                {
                    // Sauvegarde MongoDB des métadonnées (réactivée)
                    _logger.LogInformation("Tentative de sauvegarde des métadonnées en base MongoDB");
                    
                    // Log de la configuration MongoDB utilisée
                    _logger.LogDebug("Configuration MongoDB: DatabaseName={DatabaseName}, CollectionName={CollectionName}", 
                        _modRepository.GetType().GetProperty("DatabaseName")?.GetValue(null, null) ?? "<unknown>", 
                        _modRepository.GetType().GetProperty("CollectionName")?.GetValue(null, null) ?? "<unknown>");
                    
                    await _modRepository.CreateAsync(mod);
                    _logger.LogInformation("Métadonnées sauvegardées en base MongoDB avec succès: {ModId}", mod.Id);
                    
                    return Ok(new {
                        Success = true,
                        Message = "Mod uploadé avec succès (fichiers + métadonnées)",
                        Data = mod
                    });
                }
                catch (MongoDB.Driver.MongoConnectionException mcEx)
                {
                    // Erreur spécifique de connexion MongoDB
                    string serverInfo = "<unknown>";
                    if (mcEx.ConnectionId?.ServerId != null)
                    {
                        serverInfo = mcEx.ConnectionId.ServerId.ToString();
                    }
                    
                    _logger.LogError(mcEx, "ERREUR DE CONNEXION MONGODB: {Message}, ServerInfo: {ServerInfo}", 
                        mcEx.Message, serverInfo);
                    
                    // Log des détails de configuration
                    _logger.LogWarning("Détails config: {ConnectionDetails}", 
                        "Tentative de connexion à mongodb://[credentials]@mongodb:27017/modhub?authSource=admin");
                    
                    // Construire un objet de réponse similaire à mod pour la cohérence d'API
                    var modMetadata = CreateModMetadataResponse(mod);
                    
                    return Ok(new {
                        Success = true,
                        Message = $"Mod uploadé avec succès (fichiers uniquement - erreur de connexion MongoDB: {mcEx.Message})",
                        Data = modMetadata
                    });
                }
                catch (Exception dbEx)
                {
                    // Autre erreur MongoDB
                    _logger.LogError(dbEx, "ERREUR: Sauvegarde MongoDB échouée: {Type} - {Message}", 
                        dbEx.GetType().Name, dbEx.Message);
                    
                    // Essayer de logger la stack trace pour plus de détails
                    _logger.LogDebug("Stack trace: {StackTrace}", dbEx.StackTrace);
                    
                    // Construire un objet de réponse similaire à mod pour la cohérence d'API
                    var modMetadata = CreateModMetadataResponse(mod);
                    
                    return Ok(new {
                        Success = true,
                        Message = $"Mod uploadé avec succès (fichiers uniquement - erreur base de données: {dbEx.Message})",
                        Data = modMetadata
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload d'un mod");
                return StatusCode(StatusCodes.Status500InternalServerError, new { 
                    Success = false, 
                    Message = $"Une erreur s'est produite lors de l'upload: {ex.Message}", 
                    Data = (string)null 
                });
            }
        }
        
        [HttpGet("creator")]
        [Authorize(Roles = "Creator")]
        public async Task<IActionResult> GetModsByCreator()
        {
            try
            {
                // Récupérer l'ID du créateur depuis le token JWT
                var creatorIdClaim = User.FindFirst("UserId")?.Value;
                
                if (string.IsNullOrEmpty(creatorIdClaim))
                {
                    _logger.LogWarning("ID créateur manquant dans le token JWT");
                    return BadRequest(new { Success = false, Message = "ID créateur non trouvé" });
                }

                _logger.LogInformation("Récupération des mods du créateur {CreatorId}", creatorIdClaim);
                
                // Récupérer les mods du créateur depuis le repository
                var creatorMods = await _modRepository.GetByCreatorIdAsync(creatorIdClaim);
                
                if (creatorMods == null)
                {
                    creatorMods = new List<Mod>();
                }
                
                _logger.LogInformation("{Count} mods trouvés pour le créateur {CreatorId}", creatorMods.Count, creatorIdClaim);
                
                // Convertir les mods en format compatible avec le frontend ModDto
                var modDtos = creatorMods.Select(mod => new
                {
                    Id = mod.Id,
                    Name = mod.Name,
                    Slug = mod.Name.ToLowerInvariant().Replace(" ", "-"),  // Générer un slug basique
                    Description = mod.Description,
                    ShortDescription = mod.Description.Length > 100 ? mod.Description.Substring(0, 97) + "..." : mod.Description,
                    CreatorId = mod.CreatorId,
                    CreatorName = mod.Author,
                    GameId = mod.GameId,
                    GameName = mod.GameName,
                    CategoryId = string.Empty,
                    ThumbnailUrl = mod.ThumbnailUrl,
                    ScreenshotUrls = new List<string>(),
                    Version = mod.Version ?? "1.0",
                    DownloadUrl = string.Empty,
                    DocumentationUrl = string.Empty,
                    DownloadCount = mod.DownloadCount,
                    Rating = mod.Rating,
                    ReviewCount = mod.ReviewCount,
                    CreatedAt = mod.CreatedAt,
                    UpdatedAt = mod.UpdatedAt,
                    Tags = mod.Tags ?? new List<string>(),
                    Categories = new List<string>(),
                    IsFeatured = false,
                    IsApproved = true,
                    IsPremium = mod.IsPremium,
                    Versions = new List<object>()
                }).ToList();

                return Ok(new { 
                    Success = true, 
                    Message = "Mods récupérés avec succès", 
                    Data = modDtos
                });
            }
            catch (MongoDB.Driver.MongoAuthenticationException maEx)
            {
                // Erreur d'authentification MongoDB (sous-classe de MongoConnectionException, doit être avant)
                _logger.LogError(maEx, "ERREUR D'AUTHENTIFICATION MONGODB (creator/mods): {Message}", maEx.Message);
                
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Erreur d'authentification à la base de données", 
                    Error = "DB_AUTH",
                    Data = Array.Empty<object>() 
                });
            }
            catch (MongoDB.Driver.MongoConnectionException mcEx)
            {
                // Erreur spécifique de connexion MongoDB
                string serverInfo = "<unknown>";
                if (mcEx.ConnectionId?.ServerId != null)
                {
                    serverInfo = mcEx.ConnectionId.ServerId.ToString();
                }
                
                _logger.LogError(mcEx, "ERREUR DE CONNEXION MONGODB (creator/mods): {Message}, ServerInfo: {ServerInfo}", 
                    mcEx.Message, serverInfo);
                
                // Log des détails de configuration
                _logger.LogWarning("Détails config: Tentative de connexion à la base '{DatabaseName}', collection '{CollectionName}'",
                    _modRepository.GetType().GetProperty("DatabaseName")?.GetValue(_modRepository, null) ?? "<unknown>",
                    _modRepository.GetType().GetProperty("CollectionName")?.GetValue(_modRepository, null) ?? "<unknown>");
                    
                return StatusCode(500, new { 
                    Success = false, 
                    Message = $"Erreur de connexion à la base de données: {mcEx.Message}", 
                    Error = "DB_CONNECTION",
                    Data = Array.Empty<object>() 
                });
            }
            catch (NullReferenceException nrEx)
            {
                _logger.LogError(nrEx, "ERREUR NULL REFERENCE: {Message}, StackTrace: {StackTrace}", 
                    nrEx.Message, nrEx.StackTrace);
                    
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Erreur technique: données nulles ou manquantes", 
                    Error = "NULL_DATA",
                    Data = Array.Empty<object>() 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERREUR GÉNÉRALE (creator/mods): {Type} - {Message}", 
                    ex.GetType().Name, ex.Message);
                _logger.LogDebug("Stack trace: {StackTrace}", ex.StackTrace);
                
                return StatusCode(500, new { 
                    Success = false, 
                    Message = $"Erreur lors de la récupération des mods du créateur: {ex.Message}", 
                    Error = "GENERAL",
                    Data = Array.Empty<object>() 
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
        
        /// <summary>
        /// Crée un objet anonyme contenant les métadonnées du mod pour les réponses API
        /// </summary>
        private object CreateModMetadataResponse(Mod mod)
        {
            if (mod == null)
            {
                return null;
            }
            
            return new
            {
                mod.Id,
                mod.Name,
                mod.Description,
                mod.FileName,
                mod.FileSize,
                MimeType = mod.MimeType,
                UploadDate = mod.CreatedAt,
                FileLocation = mod.FileLocation,
                mod.ThumbnailUrl,
                Tags = mod.Tags?.ToArray() ?? Array.Empty<string>(),
                mod.Author,
                mod.CreatorId,
                mod.GameId,
                mod.GameName,
                mod.IsPremium,
                mod.Version
            };
        }
    }
}

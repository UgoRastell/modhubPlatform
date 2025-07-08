using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Controllers
{
    /// <summary>
    /// Contrôleur pour gérer la copie des fichiers de mods depuis ModsService vers wwwroot
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileStorageController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FileStorageController> _logger;
        private readonly IWebHostEnvironment _environment;

        public FileStorageController(
            HttpClient httpClient, 
            ILogger<FileStorageController> logger,
            IWebHostEnvironment environment)
        {
            _httpClient = httpClient;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Copie les fichiers d'un mod (thumbnail et zip) depuis ModsService vers wwwroot
        /// </summary>
        [HttpPost("copy-mod-files")]
        public async Task<IActionResult> CopyModFiles([FromBody] CopyFilesRequest request)
        {
            try
            {
                _logger.LogInformation("[FILE_STORAGE] Début de la copie des fichiers pour le mod {ModId}", request.ModId);

                // Validation des paramètres
                if (string.IsNullOrWhiteSpace(request.ModId))
                {
                    return BadRequest("ModId est requis");
                }

                // Créer le dossier de destination
                var webRootPath = _environment.WebRootPath;
                var modDirectory = Path.Combine(webRootPath, "uploads", "mods", request.ModId);
                
                _logger.LogInformation("[FILE_STORAGE] Chemin de destination: {Directory}", modDirectory);
                
                if (!Directory.Exists(modDirectory))
                {
                    Directory.CreateDirectory(modDirectory);
                    _logger.LogInformation("[FILE_STORAGE] Dossier créé: {Directory}", modDirectory);
                }

                var copiedFiles = new List<string>();

                // Copier le thumbnail si spécifié
                if (!string.IsNullOrWhiteSpace(request.ThumbnailFileName))
                {
                    var thumbnailUrl = $"https://modsservice.modhub.ovh/uploads/mods/{request.ModId}/thumbnail.jpg";
                    var thumbnailPath = Path.Combine(modDirectory, "thumbnail.jpg");
                    
                    var thumbnailCopied = await CopyFileFromUrl(thumbnailUrl, thumbnailPath, "thumbnail");
                    if (thumbnailCopied)
                    {
                        copiedFiles.Add("/uploads/mods/" + request.ModId + "/thumbnail.jpg");
                        _logger.LogInformation("[FILE_STORAGE] Thumbnail copié avec succès: {Path}", thumbnailPath);
                    }
                }

                // Copier le fichier ZIP si spécifié
                if (!string.IsNullOrWhiteSpace(request.ModFileName))
                {
                    var modFileUrl = $"https://modsservice.modhub.ovh/uploads/mods/{request.ModId}/{request.ModFileName}";
                    var modFilePath = Path.Combine(modDirectory, $"{request.ModId}.zip");
                    
                    var modFileCopied = await CopyFileFromUrl(modFileUrl, modFilePath, "mod file");
                    if (modFileCopied)
                    {
                        copiedFiles.Add($"/uploads/mods/{request.ModId}/{request.ModId}.zip");
                        _logger.LogInformation("[FILE_STORAGE] Fichier mod copié avec succès: {Path}", modFilePath);
                    }
                }

                _logger.LogInformation("[FILE_STORAGE] Copie terminée pour le mod {ModId}. {Count} fichiers copiés", 
                    request.ModId, copiedFiles.Count);

                return Ok(new CopyFilesResponse
                {
                    Success = true,
                    ModId = request.ModId,
                    CopiedFiles = copiedFiles,
                    Message = $"{copiedFiles.Count} fichier(s) copié(s) avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FILE_STORAGE] Erreur lors de la copie des fichiers pour le mod {ModId}", request.ModId);
                return StatusCode(500, new CopyFilesResponse
                {
                    Success = false,
                    ModId = request.ModId,
                    Message = $"Erreur lors de la copie: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Télécharge un fichier depuis une URL et le sauvegarde localement
        /// </summary>
        private async Task<bool> CopyFileFromUrl(string sourceUrl, string destinationPath, string fileType)
        {
            try
            {
                _logger.LogInformation("[FILE_STORAGE] Téléchargement {FileType} depuis: {Url}", fileType, sourceUrl);
                
                var response = await _httpClient.GetAsync(sourceUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[FILE_STORAGE] Échec du téléchargement {FileType}: {StatusCode}", 
                        fileType, response.StatusCode);
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Vérifier que le fichier n'est pas la page HTML d'erreur
                if (fileBytes.Length < 10000 && System.Text.Encoding.UTF8.GetString(fileBytes).Contains("<!DOCTYPE html"))
                {
                    _logger.LogWarning("[FILE_STORAGE] Le fichier {FileType} retourné semble être une page HTML", fileType);
                    return false;
                }

                await System.IO.File.WriteAllBytesAsync(destinationPath, fileBytes);
                
                _logger.LogInformation("[FILE_STORAGE] {FileType} sauvegardé: {Path} ({Size} bytes)", 
                    fileType, destinationPath, fileBytes.Length);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FILE_STORAGE] Erreur lors de la copie du {FileType} depuis {Url}", fileType, sourceUrl);
                return false;
            }
        }
    }

    /// <summary>
    /// Requête pour copier les fichiers d'un mod
    /// </summary>
    public class CopyFilesRequest
    {
        [Required]
        public string ModId { get; set; } = string.Empty;
        
        public string? ThumbnailFileName { get; set; }
        
        public string? ModFileName { get; set; }
    }

    /// <summary>
    /// Réponse de la copie des fichiers
    /// </summary>
    public class CopyFilesResponse
    {
        public bool Success { get; set; }
        public string ModId { get; set; } = string.Empty;
        public List<string> CopiedFiles { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}

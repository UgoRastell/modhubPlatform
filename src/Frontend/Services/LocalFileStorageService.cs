using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.Services
{
    /// <summary>
    /// Service pour gérer le stockage des fichiers de mods - version simplifiée
    /// Utilise l'upload traditionnel vers ModsService mais adapte les URLs pour wwwroot
    /// </summary>
    public class LocalFileStorageService : ILocalFileStorageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(HttpClient httpClient, ILogger<LocalFileStorageService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Upload le fichier mod vers le backend et copie dans wwwroot
        /// </summary>
        public async Task<string> SaveModFileAsync(string modId, IBrowserFile modFile)
        {
            try
            {
                _logger.LogInformation("Copie du fichier mod {ModId}: {FileName} vers wwwroot", modId, modFile.Name);
                
                // Créer le dossier de destination
                var modDirectory = Path.Combine("wwwroot", "uploads", "mods", modId);
                if (!Directory.Exists(modDirectory))
                {
                    Directory.CreateDirectory(modDirectory);
                    _logger.LogInformation("Dossier créé: {Directory}", modDirectory);
                }
                
                // Nom du fichier de destination
                var fileName = $"{modId}.zip";
                var filePath = Path.Combine(modDirectory, fileName);
                
                // Copier le fichier
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var modStream = modFile.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50 MB max
                await modStream.CopyToAsync(fileStream);
                
                _logger.LogInformation("Fichier mod copié avec succès: {FilePath}", filePath);
                
                // URL standardisée pour l'accès au fichier
                return $"/uploads/mods/{modId}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la copie du fichier mod {ModId}", modId);
                throw;
            }
        }

        /// <summary>
        /// Upload le thumbnail vers le backend et copie dans wwwroot
        /// </summary>
        public async Task<string> SaveThumbnailAsync(string modId, IBrowserFile thumbnailFile)
        {
            try
            {
                _logger.LogInformation("Copie du thumbnail {ModId}: {FileName} vers wwwroot", modId, thumbnailFile.Name);
                
                // Créer le dossier de destination
                var modDirectory = Path.Combine("wwwroot", "uploads", "mods", modId);
                if (!Directory.Exists(modDirectory))
                {
                    Directory.CreateDirectory(modDirectory);
                    _logger.LogInformation("Dossier créé: {Directory}", modDirectory);
                }
                
                // Déterminer l'extension du fichier
                var extension = Path.GetExtension(thumbnailFile.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                
                // Nom du fichier de destination
                var fileName = $"thumbnail{extension}";
                var filePath = Path.Combine(modDirectory, fileName);
                
                // Copier le fichier
                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                using var thumbnailStream = thumbnailFile.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5 MB max
                await thumbnailStream.CopyToAsync(fileStream);
                
                _logger.LogInformation("Thumbnail copié avec succès: {FilePath}", filePath);
                
                // URL standardisée pour l'accès au thumbnail
                return $"/uploads/mods/{modId}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la copie du thumbnail {ModId}", modId);
                throw;
            }
        }

        /// <summary>
        /// Supprime les fichiers d'un mod (délégué vers l'API)
        /// </summary>
        public async Task<bool> DeleteModFilesAsync(string modId)
        {
            try
            {
                // Appel à l'API de suppression de mod existante
                var response = await _httpClient.DeleteAsync($"api/v1/mods/{modId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression des fichiers du mod {ModId}", modId);
                return false;
            }
        }

        /// <summary>
        /// Retourne l'URL du thumbnail d'un mod (compatible wwwroot)
        /// </summary>
        public string GetThumbnailUrl(string modId)
        {
            return $"/uploads/mods/{modId}/thumbnail.jpg";
        }

        /// <summary>
        /// Retourne l'URL de téléchargement d'un mod (compatible wwwroot)
        /// </summary>
        public string GetModDownloadUrl(string modId)
        {
            return $"/uploads/mods/{modId}/{modId}.zip";
        }
    }
}

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
        /// Upload le fichier mod vers le backend et retourne l'URL d'accès
        /// </summary>
        public Task<string> SaveModFileAsync(string modId, IBrowserFile modFile)
        {
            try
            {
                // Pour l'instant, on retourne juste l'URL attendue
                // L'upload réel sera fait par Upload.razor vers l'API existante
                _logger.LogInformation("Préparation du stockage du fichier mod {ModId}: {FileName}", modId, modFile.Name);
                
                // URL standardisée pour l'accès au fichier
                return Task.FromResult($"/uploads/mods/{modId}/{modId}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la préparation du stockage du fichier mod {ModId}", modId);
                throw;
            }
        }

        /// <summary>
        /// Upload le thumbnail vers le backend et retourne l'URL d'accès
        /// </summary>
        public Task<string> SaveThumbnailAsync(string modId, IBrowserFile thumbnailFile)
        {
            try
            {
                // Pour l'instant, on retourne juste l'URL attendue
                // L'upload réel sera fait par Upload.razor vers l'API existante
                _logger.LogInformation("Préparation du stockage du thumbnail {ModId}: {FileName}", modId, thumbnailFile.Name);
                
                // URL standardisée pour l'accès au thumbnail
                var extension = Path.GetExtension(thumbnailFile.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                
                return Task.FromResult($"/uploads/mods/{modId}/thumbnail{extension}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la préparation du stockage du thumbnail {ModId}", modId);
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

using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Json;
using System.Text.Json;
using Frontend.Controllers;

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
        /// Upload le fichier mod vers le backend et copie dans wwwroot via l'API
        /// </summary>
        public async Task<string> SaveModFileAsync(string modId, IBrowserFile modFile)
        {
            try
            {
                _logger.LogInformation("Demande de copie du fichier mod {ModId}: {FileName} via API backend", modId, modFile.Name);
                
                // Appeler l'endpoint backend pour copier le fichier
                var request = new CopyFilesRequest
                {
                    ModId = modId,
                    ModFileName = modFile.Name
                };
                
                var response = await _httpClient.PostAsJsonAsync("/api/filestorage/copy-mod-files", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CopyFilesResponse>();
                    _logger.LogInformation("Fichier mod copié avec succès via API: {ModId}", modId);
                    
                    // URL standardisée pour l'accès au fichier
                    return $"/uploads/mods/{modId}/{modId}.zip";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur API lors de la copie du fichier mod {ModId}: {Error}", modId, error);
                    throw new Exception($"Erreur API: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la copie du fichier mod {ModId}", modId);
                throw;
            }
        }

        /// <summary>
        /// Upload le thumbnail vers le backend et copie dans wwwroot via l'API
        /// </summary>
        public async Task<string> SaveThumbnailAsync(string modId, IBrowserFile thumbnailFile)
        {
            try
            {
                _logger.LogInformation("Demande de copie du thumbnail {ModId}: {FileName} via API backend", modId, thumbnailFile.Name);
                
                // Appeler l'endpoint backend pour copier le thumbnail
                var request = new CopyFilesRequest
                {
                    ModId = modId,
                    ThumbnailFileName = thumbnailFile.Name
                };
                
                var response = await _httpClient.PostAsJsonAsync("/api/filestorage/copy-mod-files", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<CopyFilesResponse>();
                    _logger.LogInformation("Thumbnail copié avec succès via API: {ModId}", modId);
                    
                    // URL standardisée pour l'accès au thumbnail
                    return $"/uploads/mods/{modId}/thumbnail.jpg";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Erreur API lors de la copie du thumbnail {ModId}: {Error}", modId, error);
                    throw new Exception($"Erreur API: {response.StatusCode} - {error}");
                }
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

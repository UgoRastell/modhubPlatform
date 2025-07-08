using Microsoft.AspNetCore.Components.Forms;

namespace Frontend.Services
{
    /// <summary>
    /// Service simplifié pour gérer les URLs des fichiers de mods
    /// Avec l'architecture volume Docker partagé, les fichiers sont synchronisés automatiquement
    /// </summary>
    public class LocalFileStorageService : ILocalFileStorageService
    {
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retourne l'URL du fichier mod (synchronisation automatique via Docker)
        /// </summary>
        public Task<string> SaveModFileAsync(string modId, IBrowserFile modFile)
        {
            // La synchronisation est automatique via volume Docker partagé
            // ModsService écrit dans /app/uploads/mods/{modId}/ 
            // Frontend lit depuis /app/wwwroot/uploads/mods/{modId}/
            _logger.LogInformation("URL statique générée pour le mod {ModId}: {FileName}", modId, modFile.Name);
            return Task.FromResult($"/uploads/mods/{modId}/{modId}.zip");
        }

        /// <summary>
        /// Retourne l'URL du thumbnail (synchronisation automatique via Docker)
        /// </summary>
        public Task<string> SaveThumbnailAsync(string modId, IBrowserFile thumbnailFile)
        {
            // La synchronisation est automatique via volume Docker partagé
            _logger.LogInformation("URL statique générée pour le thumbnail {ModId}: {FileName}", modId, thumbnailFile.Name);
            return Task.FromResult($"/uploads/mods/{modId}/thumbnail.jpg");
        }

        /// <summary>
        /// Simulation de suppression (délégué vers l'API ModsService)
        /// </summary>
        public Task<bool> DeleteModFilesAsync(string modId)
        {
            _logger.LogInformation("Suppression déléguée vers ModsService pour le mod {ModId}", modId);
            // La suppression est gérée par ModsService via l'API de suppression
            return Task.FromResult(true);
        }

        /// <summary>
        /// Retourne l'URL du thumbnail d'un mod (accès statique)
        /// </summary>
        public string GetThumbnailUrl(string modId)
        {
            return $"/uploads/mods/{modId}/thumbnail.jpg";
        }

        /// <summary>
        /// Retourne l'URL de téléchargement d'un mod (accès statique)
        /// </summary>
        public string GetModDownloadUrl(string modId)
        {
            return $"/uploads/mods/{modId}/{modId}.zip";
        }
    }
}

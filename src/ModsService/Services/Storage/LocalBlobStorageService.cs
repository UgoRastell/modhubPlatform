using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ModsService.Services.Storage
{
    /// <summary>
    /// Implémentation du service de stockage blob utilisant le système de fichiers local
    /// </summary>
    public class LocalBlobStorageService : IBlobStorageService
    {
        private readonly BlobStorageSettings _settings;
        private readonly ILogger<LocalBlobStorageService> _logger;
        private readonly FileValidationService _fileValidationService;

        public LocalBlobStorageService(
            IOptions<BlobStorageSettings> settings,
            ILogger<LocalBlobStorageService> logger,
            FileValidationService fileValidationService)
        {
            _settings = settings.Value;
            _logger = logger;
            _fileValidationService = fileValidationService;
        }
        
        /// <inheritdoc />
        public async Task<ModFile> UploadFileAsync(IFormFile file, string containerName, string filePath = null)
        {
            try
            {
                using var stream = file.OpenReadStream();
                return await UploadFileAsync(stream, file.FileName, file.ContentType, containerName, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléversement du fichier {FileName} vers {Container}", file.FileName, containerName);
                throw new IOException($"Le téléversement du fichier {file.FileName} a échoué : {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<ModFile> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName, string filePath = null)
        {
            try
            {
                // Validation de base
                await _fileValidationService.ValidateFileAsync(fileStream, fileName, contentType);
                
                // Réinitialiser la position du stream pour pouvoir le lire à nouveau
                fileStream.Position = 0;
                
                // Préparer le chemin de stockage
                var storageDirectory = Path.Combine(_settings.LocalStoragePath, SafeContainerName(containerName));
                
                // Créer un nom de fichier unique basé sur GUID si aucun chemin n'est spécifié
                var storagePath = filePath ?? $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
                
                // Chemin complet du fichier
                var fullPath = Path.Combine(storageDirectory, storagePath);
                
                // S'assurer que le répertoire existe
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                
                // Calculer les hachages MD5 et SHA-256 pendant la copie
                string md5Hash, sha256Hash;
                long fileSize;
                
                using (var outputStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var md5 = MD5.Create())
                using (var sha256 = SHA256.Create())
                using (var cryptoStream = new CryptoStream(
                    new CryptoStream(outputStream, md5, CryptoStreamMode.Write),
                    sha256, CryptoStreamMode.Write))
                {
                    await fileStream.CopyToAsync(cryptoStream);
                    await cryptoStream.FlushAsync();
                    
                    // Obtenir la taille finale du fichier
                    fileSize = outputStream.Length;
                    
                    // Convertir les hachages en chaînes hexadécimales
                    md5Hash = BitConverter.ToString(md5.Hash).Replace("-", "").ToLowerInvariant();
                    sha256Hash = BitConverter.ToString(sha256.Hash).Replace("-", "").ToLowerInvariant();
                }
                
                // Créer l'URL de téléchargement
                var downloadUrl = await GetFileUrlAsync(storagePath, containerName);
                
                // Créer et retourner l'objet ModFile
                var modFile = new ModFile
                {
                    FileName = fileName,
                    ContentType = contentType,
                    SizeBytes = fileSize,
                    Md5Hash = md5Hash,
                    Sha256Hash = sha256Hash,
                    StoragePath = storagePath,
                    DownloadUrl = downloadUrl,
                    UploadedAt = DateTime.UtcNow,
                    IsApproved = false // Par défaut, les fichiers ne sont pas approuvés jusqu'à ce qu'ils soient scannés
                };
                
                // Stocker les métadonnées du fichier
                var metadata = new Dictionary<string, string>
                {
                    { "OriginalFileName", fileName },
                    { "ContentType", contentType },
                    { "Size", fileSize.ToString() },
                    { "MD5", md5Hash },
                    { "SHA256", sha256Hash },
                    { "UploadedAt", DateTime.UtcNow.ToString("o") }
                };
                
                await SetFileMetadataAsync(storagePath, containerName, metadata);
                
                return modFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléversement du fichier {FileName} vers {Container}", fileName, containerName);
                throw new IOException($"Le téléversement du fichier {fileName} a échoué : {ex.Message}");
            }
        }

        /// <inheritdoc />
        public Task<Stream> DownloadFileAsync(string filePath, string containerName)
        {
            try
            {
                var fullPath = GetFullPath(filePath, containerName);
                
                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Le fichier {filePath} n'a pas été trouvé dans {containerName}");
                }
                
                // Ouvrir le fichier en mode lecture
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return Task.FromResult<Stream>(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du fichier {FilePath} depuis {Container}", filePath, containerName);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<string> GetFileUrlAsync(string filePath, string containerName, TimeSpan? expiryTime = null)
        {
            var safeContainer = SafeContainerName(containerName);
            var urlPath = $"{_settings.BaseUrl}/{safeContainer}/{filePath}";
            
            // Note: Pour une implémentation locale, nous ne générons pas d'URL signée
            // Dans une implémentation cloud, nous utiliserions des jetons SAS ou des signatures
            return Task.FromResult(urlPath);
        }

        /// <inheritdoc />
        public Task<bool> FileExistsAsync(string filePath, string containerName)
        {
            var fullPath = GetFullPath(filePath, containerName);
            return Task.FromResult(File.Exists(fullPath));
        }

        /// <inheritdoc />
        public Task<bool> DeleteFileAsync(string filePath, string containerName)
        {
            try
            {
                var fullPath = GetFullPath(filePath, containerName);
                
                if (!File.Exists(fullPath))
                {
                    return Task.FromResult(false);
                }
                
                File.Delete(fullPath);
                
                // Supprimer également le fichier de métadonnées s'il existe
                var metadataPath = GetMetadataPath(filePath, containerName);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }
                
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier {FilePath} de {Container}", filePath, containerName);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> ListFilesAsync(string containerName, string prefix = null)
        {
            try
            {
                var directory = Path.Combine(_settings.LocalStoragePath, SafeContainerName(containerName));
                
                if (!Directory.Exists(directory))
                {
                    return Task.FromResult(Enumerable.Empty<string>());
                }
                
                var searchPattern = prefix == null ? "*" : $"{prefix}*";
                var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)
                    .Select(f => f.Substring(directory.Length + 1).Replace('\\', '/'));
                
                return Task.FromResult(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du listage des fichiers dans {Container} avec préfixe {Prefix}", containerName, prefix);
                return Task.FromResult(Enumerable.Empty<string>());
            }
        }

        /// <inheritdoc />
        public Task<Dictionary<string, string>> GetFileMetadataAsync(string filePath, string containerName)
        {
            try
            {
                var metadataPath = GetMetadataPath(filePath, containerName);
                
                if (!File.Exists(metadataPath))
                {
                    return Task.FromResult(new Dictionary<string, string>());
                }
                
                var metadataJson = File.ReadAllText(metadataPath);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson);
                
                return Task.FromResult(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métadonnées pour {FilePath} dans {Container}", filePath, containerName);
                return Task.FromResult(new Dictionary<string, string>());
            }
        }

        /// <inheritdoc />
        public Task SetFileMetadataAsync(string filePath, string containerName, Dictionary<string, string> metadata)
        {
            try
            {
                var metadataPath = GetMetadataPath(filePath, containerName);
                
                // S'assurer que le répertoire existe
                Directory.CreateDirectory(Path.GetDirectoryName(metadataPath));
                
                var metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
                File.WriteAllText(metadataPath, metadataJson);
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la définition des métadonnées pour {FilePath} dans {Container}", filePath, containerName);
                throw;
            }
        }
        
        private string GetFullPath(string filePath, string containerName)
        {
            return Path.Combine(_settings.LocalStoragePath, SafeContainerName(containerName), filePath);
        }
        
        private string GetMetadataPath(string filePath, string containerName)
        {
            // Stocker les métadonnées dans un sous-dossier caché
            return Path.Combine(_settings.LocalStoragePath, SafeContainerName(containerName), ".metadata", $"{filePath}.meta.json");
        }
        
        private string SafeContainerName(string containerName)
        {
            // Normaliser le nom du conteneur pour qu'il soit sûr comme nom de dossier
            return containerName.ToLowerInvariant().Replace(" ", "-").Replace(".", "-");
        }
    }
}

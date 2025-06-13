using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace ModsService.Services.Storage
{
    /// <summary>
    /// Service de validation des fichiers téléversés
    /// </summary>
    public class FileValidationService
    {
        private readonly BlobStorageSettings _settings;
        private readonly ILogger<FileValidationService> _logger;
        
        // Liste des signatures de fichiers pour la détection de types
        private static readonly Dictionary<string, List<byte[]>> FileSignatures = new Dictionary<string, List<byte[]>>
        {
            { ".zip", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
            { ".rar", new List<byte[]> { 
                    new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }, // RAR5
                    new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 } // RAR4
                } 
            },
            { ".7z", new List<byte[]> { new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C } } }
        };

        public FileValidationService(IOptions<BlobStorageSettings> settings, ILogger<FileValidationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Valide un fichier selon les critères configurés (taille, extension, format)
        /// </summary>
        /// <param name="fileStream">Flux du fichier</param>
        /// <param name="fileName">Nom du fichier</param>
        /// <param name="contentType">Type de contenu</param>
        /// <returns>Task</returns>
        public async Task ValidateFileAsync(Stream fileStream, string fileName, string contentType)
        {
            // Vérifier la taille du fichier
            if (fileStream.Length > _settings.MaxFileSizeMB * 1024 * 1024)
            {
                throw new ArgumentException($"Le fichier dépasse la taille maximale autorisée de {_settings.MaxFileSizeMB} MB");
            }

            // Vérifier l'extension du fichier
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            string[] allowedExtensions = _settings.AllowedExtensions.Split(',').Select(e => e.Trim().ToLowerInvariant()).ToArray();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"L'extension de fichier '{extension}' n'est pas autorisée. Extensions autorisées: {_settings.AllowedExtensions}");
            }

            // Vérifier la signature du fichier
            if (!await IsValidFileTypeAsync(fileStream, extension))
            {
                throw new ArgumentException($"Le contenu du fichier ne correspond pas à l'extension déclarée '{extension}'");
            }

            // Vérifier si l'archive peut être ouverte (non corrompue)
            if (!await CanOpenArchiveAsync(fileStream, extension))
            {
                throw new ArgumentException($"L'archive '{fileName}' est corrompue ou dans un format non supporté");
            }
            
            // Réinitialiser la position du stream pour une utilisation ultérieure
            fileStream.Position = 0;
        }

        /// <summary>
        /// Vérifie si le fichier a une signature valide correspondant à son extension
        /// </summary>
        private async Task<bool> IsValidFileTypeAsync(Stream fileStream, string extension)
        {
            // Sauvegarder la position actuelle
            var originalPosition = fileStream.Position;
            
            try
            {
                // Vérifier si l'extension est dans notre dictionnaire de signatures
                if (!FileSignatures.TryGetValue(extension, out var signatures))
                {
                    // Si nous n'avons pas de signature définie pour cette extension,
                    // on accepte le fichier (mais on a déjà vérifié l'extension autorisée)
                    return true;
                }

                // Trouver la taille maximale de signature à vérifier
                int maxSignatureSize = signatures.Max(s => s.Length);
                
                // Lire assez d'octets pour vérifier toutes les signatures possibles
                var buffer = new byte[maxSignatureSize];
                fileStream.Position = 0;
                
                await fileStream.ReadAsync(buffer, 0, maxSignatureSize);
                
                // Vérifier si l'une des signatures correspond
                foreach (var signature in signatures)
                {
                    bool isMatch = true;
                    
                    for (int i = 0; i < signature.Length; i++)
                    {
                        if (buffer[i] != signature[i])
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    
                    if (isMatch)
                    {
                        return true;
                    }
                }
                
                // Aucune signature ne correspond
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification du type de fichier");
                return false;
            }
            finally
            {
                // Restaurer la position du stream
                fileStream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Vérifie si l'archive peut être ouverte
        /// </summary>
        private async Task<bool> CanOpenArchiveAsync(Stream fileStream, string extension)
        {
            // Sauvegarder la position actuelle
            var originalPosition = fileStream.Position;
            
            try
            {
                fileStream.Position = 0;
                
                switch (extension)
                {
                    case ".zip":
                        // Essayer d'ouvrir comme archive ZIP
                        using (var zipArchive = new ZipArchive(fileStream, ZipArchiveMode.Read, leaveOpen: true))
                        {
                            // Vérifier qu'il y a au moins une entrée
                            return zipArchive.Entries.Count > 0;
                        }

                    case ".rar":
                    case ".7z":
                        // Essayer d'ouvrir comme archive RAR ou 7Z avec SharpCompress
                        using (var archive = ArchiveFactory.Open(fileStream, new ReaderOptions { LeaveStreamOpen = true }))
                        {
                            // Vérifier qu'il y a au moins une entrée
                            return archive.Entries.Any();
                        }
                        
                    default:
                        // Type non géré, accepter par défaut
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'intégrité de l'archive: {Message}", ex.Message);
                return false;
            }
            finally
            {
                // Restaurer la position du stream
                fileStream.Position = originalPosition;
            }
        }
    }
}

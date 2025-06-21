using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Repositories;
using ModsService.Services.Security;
using ModsService.Services.Storage;
using Shared.Models;

namespace ModsService.Services
{
    /// <summary>
    /// Service gérant le versioning des mods et l'upload sécurisé des fichiers
    /// Complément au ModService existant
    /// </summary>
    public class ModVersioningService : IVersioningService
    {
        private readonly IModRepository _modRepository;
        private readonly IBlobStorageService _blobStorageService;
        private readonly ISecurityScanService _securityScanService;
        private readonly IVersionHistoryService _versionHistoryService;
        private readonly ILogger<ModVersioningService> _logger;
        
        public ModVersioningService(
            IModRepository modRepository,
            IBlobStorageService blobStorageService,
            ISecurityScanService securityScanService,
            IVersionHistoryService versionHistoryService,
            ILogger<ModVersioningService> logger)
        {
            _modRepository = modRepository;
            _blobStorageService = blobStorageService;
            _securityScanService = securityScanService;
            _versionHistoryService = versionHistoryService;
            _logger = logger;
        }
        
        /// <summary>
        /// Ajoute une nouvelle version à un mod existant
        /// </summary>
        public async Task<ApiResponse<ModVersion>> AddModVersionAsync(string modId, ModVersion version, IFormFile file = null)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<ModVersion>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Vérifier si la version existe déjà
                if (mod.Versions.Any(v => v.VersionNumber == version.VersionNumber))
                {
                    return new ApiResponse<ModVersion>
                    {
                        Success = false,
                        Message = $"La version {version.VersionNumber} existe déjà pour ce mod"
                    };
                }
                
                // Attribuer un nouvel ID unique à la version
                version.Id = Guid.NewGuid().ToString();
                version.CreatedAt = DateTime.UtcNow;
                version.ModId = modId;
                
                // Traiter le fichier s'il est fourni
                if (file != null)
                {
                    var fileUploadResult = await UploadAndScanModFileAsync(modId, version.Id, file);
                    
                    if (!fileUploadResult.Success)
                    {
                        return new ApiResponse<ModVersion>
                        {
                            Success = false,
                            Message = fileUploadResult.Message
                        };
                    }
                    
                    // Ajouter les informations du fichier à la version
                    version.MainFile = fileUploadResult.Data;
                }
                
                // Ajouter la version au mod
                mod.Versions.Add(version);
                
                // Mettre à jour le mod avec la nouvelle version
                await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour l'historique des versions
                await _versionHistoryService.UpdateVersionHistoryAsync(mod, version);
                
                // Générer un changelog combiné si nécessaire
                if (!string.IsNullOrEmpty(version.Changelog))
                {
                    mod.CombinedChangelog = await _versionHistoryService.GenerateCombinedChangelogAsync(mod);
                    await _modRepository.UpdateModAsync(mod);
                }
                
                return new ApiResponse<ModVersion>
                {
                    Success = true,
                    Message = $"Version {version.VersionNumber} ajoutée avec succès",
                    Data = version
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'ajout de la version pour le mod {modId}");
                
                return new ApiResponse<ModVersion>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Met à jour une version existante d'un mod
        /// </summary>
        public async Task<ApiResponse<ModVersion>> UpdateModVersionAsync(string modId, string versionId, ModVersion updatedVersion, IFormFile file = null)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<ModVersion>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }

                // Trouver la version à mettre à jour
                var existingVersionIndex = mod.Versions.FindIndex(v => v.Id == versionId);
                
                if (existingVersionIndex == -1)
                {
                    return new ApiResponse<ModVersion>
                    {
                        Success = false,
                        Message = $"Version avec ID {versionId} non trouvée pour ce mod"
                    };
                }
                
                var existingVersion = mod.Versions[existingVersionIndex];
                
                // Préserver les champs qui ne doivent pas être modifiés
                updatedVersion.Id = existingVersion.Id;
                updatedVersion.CreatedAt = existingVersion.CreatedAt;
                updatedVersion.ModId = modId;
                updatedVersion.MainFile = existingVersion.MainFile; // On garde le fichier existant sauf si un nouveau est fourni
                updatedVersion.DownloadCount = existingVersion.DownloadCount;
                
                // Mettre à jour la date de modification
                updatedVersion.UpdatedAt = DateTime.UtcNow;
                
                // Traiter le fichier s'il est fourni
                if (file != null)
                {
                    var fileUploadResult = await UploadAndScanModFileAsync(modId, versionId, file);
                    
                    if (!fileUploadResult.Success)
                    {
                        return new ApiResponse<ModVersion>
                        {
                            Success = false,
                            Message = fileUploadResult.Message
                        };
                    }
                    
                    // Mettre à jour les informations du fichier
                    updatedVersion.MainFile = fileUploadResult.Data;
                }
                
                // Remplacer la version dans la liste
                mod.Versions[existingVersionIndex] = updatedVersion;
                
                // Mettre à jour le mod avec la version modifiée
                await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour l'historique des versions si le changelog a été modifié
                if (existingVersion.Changelog != updatedVersion.Changelog)
                {
                    await _versionHistoryService.UpdateVersionHistoryAsync(mod, updatedVersion);
                    
                    // Régénérer le changelog combiné
                    mod.CombinedChangelog = await _versionHistoryService.GenerateCombinedChangelogAsync(mod);
                    await _modRepository.UpdateModAsync(mod);
                }
                
                return new ApiResponse<ModVersion>
                {
                    Success = true,
                    Message = $"Version {updatedVersion.VersionNumber} mise à jour avec succès",
                    Data = updatedVersion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la mise à jour de la version {versionId} pour le mod {modId}");
                
                return new ApiResponse<ModVersion>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Supprime une version d'un mod
        /// </summary>
        public async Task<ApiResponse<bool>> DeleteModVersionAsync(string modId, string versionId)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Mod non trouvé",
                        Data = false
                    };
                }

                // Vérifier qu'il reste au moins une version après la suppression
                if (mod.Versions.Count <= 1)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Impossible de supprimer la dernière version d'un mod. Supprimez le mod entier si nécessaire.",
                        Data = false
                    };
                }
                
                // Trouver la version à supprimer
                var version = mod.Versions.FirstOrDefault(v => v.Id == versionId);
                
                if (version == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = $"Version avec ID {versionId} non trouvée pour ce mod",
                        Data = false
                    };
                }
                
                // Supprimer les fichiers associés
                if (version.MainFile != null)
                {
                    await DeleteModFileAsync(modId, versionId);
                }
                
                // Supprimer la version du mod
                mod.Versions.Remove(version);
                
                // Mettre à jour le mod sans la version supprimée
                await _modRepository.UpdateModAsync(mod);
                
                // Mettre à jour l'historique des versions
                await _versionHistoryService.RemoveVersionFromHistoryAsync(mod.Id, versionId);
                
                // Régénérer le changelog combiné
                mod.CombinedChangelog = await _versionHistoryService.GenerateCombinedChangelogAsync(mod);
                await _modRepository.UpdateModAsync(mod);
                
                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = $"Version {version.VersionNumber} supprimée avec succès",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la version {versionId} pour le mod {modId}");
                
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}",
                    Data = false
                };
            }
        }
        
        /// <summary>
        /// Génère un changelog entre deux versions d'un mod
        /// </summary>
        public async Task<ApiResponse<string>> GenerateChangelogAsync(string modId, string fromVersion, string toVersion)
        {
            try
            {
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Mod non trouvé"
                    };
                }
                
                // Générer le changelog comparatif
                var changelog = await _versionHistoryService.GenerateComparedChangelogAsync(mod, fromVersion, toVersion);
                
                return new ApiResponse<string>
                {
                    Success = true,
                    Message = "Changelog généré avec succès",
                    Data = changelog
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la génération du changelog pour le mod {modId}");
                
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Télécharge et valide un fichier de mod, puis effectue un scan de sécurité
        /// </summary>
        private async Task<ApiResponse<ModFile>> UploadAndScanModFileAsync(string modId, string versionId, IFormFile file)
        {
            try
            {
                // Vérifier le type de fichier
                var allowedExtensions = new[] { ".zip", ".rar", ".7z" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new ApiResponse<ModFile>
                    {
                        Success = false,
                        Message = "Type de fichier non autorisé. Les formats acceptés sont: .zip, .rar, .7z"
                    };
                }
                
                // Vérifier la taille du fichier (100MB max par défaut)
                var maxFileSizeMB = 100;
                if (file.Length > maxFileSizeMB * 1024 * 1024)
                {
                    return new ApiResponse<ModFile>
                    {
                        Success = false,
                        Message = $"Le fichier dépasse la taille maximale autorisée de {maxFileSizeMB}MB"
                    };
                }
                
                // Créer un nom de fichier unique basé sur l'ID du mod et de la version
                var fileName = $"{modId}_{versionId}{fileExtension}";
                var blobPath = $"mods/{modId}/versions/{versionId}/files/main";
                
                // Sauvegarder le fichier dans le stockage
                using (var stream = file.OpenReadStream())
                {
                    await _blobStorageService.UploadBlobAsync(blobPath, stream);
                }
                
                // Effectuer un scan de sécurité sur le fichier
                var scanResult = await _securityScanService.ScanFileAsync(await _blobStorageService.GetBlobAsync(blobPath));
                
                // Créer l'objet ModFile
                var modFile = new ModFile
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    SizeInBytes = file.Length,
                    StoragePath = blobPath,
                    UploadedAt = DateTime.UtcNow,
                    Hash = await ComputeFileHashAsync(file),
                    FileExtension = fileExtension,
                    SecurityScan = new SecurityScan
                    {
                        ScanId = scanResult.ScanId,
                        Status = scanResult.Status,
                        ScanDate = scanResult.ScanDate,
                        ThreatDetected = scanResult.ThreatDetected,
                        ThreatName = scanResult.ThreatName,
                        ScanDetails = scanResult.ScanDetails
                    }
                };

                // Vérifier si le fichier est sécurisé
                if (scanResult.ThreatDetected)
                {
                    // Supprimer le fichier malveillant
                    await _blobStorageService.DeleteBlobAsync(blobPath);
                    
                    return new ApiResponse<ModFile>
                    {
                        Success = false,
                        Message = $"Menace détectée dans le fichier: {scanResult.ThreatName}. Fichier rejeté."
                    };
                }
                
                return new ApiResponse<ModFile>
                {
                    Success = true,
                    Message = "Fichier téléchargé et validé avec succès",
                    Data = modFile
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du téléchargement du fichier pour le mod {modId}, version {versionId}");
                
                return new ApiResponse<ModFile>
                {
                    Success = false,
                    Message = $"Une erreur est survenue: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Supprime un fichier de mod du stockage
        /// </summary>
        private async Task<bool> DeleteModFileAsync(string modId, string versionId)
        {
            try
            {
                var blobPath = $"mods/{modId}/versions/{versionId}/files/main";
                
                // Vérifier si le fichier existe
                if (await _blobStorageService.ExistsAsync(blobPath))
                {
                    // Supprimer le fichier
                    await _blobStorageService.DeleteBlobAsync(blobPath);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression du fichier pour le mod {modId}, version {versionId}");
                return false;
            }
        }
        
        /// <summary>
        /// Calcule le hash d'un fichier pour la vérification d'intégrité
        /// </summary>
        private async Task<string> ComputeFileHashAsync(IFormFile file)
        {
            using (var stream = file.OpenReadStream())
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashBytes = await sha256.ComputeHashAsync(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

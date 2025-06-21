using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsService.Models;
using ModsService.Services.Storage;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModsService.Services.Security
{
    /// <summary>
    /// Service d'analyse de sécurité pour les fichiers téléversés
    /// </summary>
    public class SecurityScanService : ISecurityScanService
    {
        private readonly ILogger<SecurityScanService> _logger;
        private readonly BlobStorageSettings _blobSettings;
        private readonly IBlobStorageService _blobStorageService;
        
        // Liste des extensions de fichiers potentiellement dangereuses
        private static readonly string[] SuspiciousExtensions = new[] 
        { 
            ".exe", ".dll", ".bat", ".cmd", ".ps1", ".vbs", ".js", ".msi", 
            ".scr", ".pif", ".jar", ".com", ".hta", ".cpl", ".reg"
        };
        
        // Expressions régulières pour détecter du code potentiellement malveillant
        private static readonly List<(string Pattern, string Description)> SuspiciousPatterns = new List<(string, string)>
        {
            (@"(?i)(Win32|Win64)\.Trojan|Backdoor\.", "Signature de trojan/backdoor détectée"),
            (@"(?i)cmd\.exe\s+/c", "Commande shell potentiellement malveillante"),
            (@"(?i)powershell\s+-ExecutionPolicy\s+Bypass", "Commande PowerShell contournant la politique d'exécution"),
            (@"(?i)(Start-Process|Invoke-Expression|IEX)\s*\(", "PowerShell avec exécution dynamique"),
            (@"(?i)net\s+localgroup\s+administrators", "Tentative de modification des groupes système"),
            (@"(?i)schtasks\s+/create", "Tentative de création de tâche planifiée"),
            (@"(?i)reg\s+add\s+HKEY_", "Tentative de modification du registre")
        };

        public SecurityScanService(
            ILogger<SecurityScanService> logger,
            IOptions<BlobStorageSettings> blobSettings,
            IBlobStorageService blobStorageService)
        {
            _logger = logger;
            _blobSettings = blobSettings.Value;
            _blobStorageService = blobStorageService;
        }

        /// <inheritdoc />
        public async Task<SecurityScan> ScanFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                _logger.LogInformation("Démarrage du scan de sécurité pour le fichier {FileName}", fileName);
                
                // Créer un nouveau scan avec statut initial "en cours"
                var scan = new SecurityScan
                {
                    ScannedAt = DateTime.UtcNow,
                    Status = ScanStatus.Scanning,
                    ScanEngine = "ModHub Security Scanner",
                    ScanEngineVersion = "1.0.0"
                };
                
                // Sauvegarder la position originale du stream
                var originalPosition = fileStream.Position;
                fileStream.Position = 0;
                
                // Vérifier si c'est une archive
                bool isArchive = IsArchiveFile(fileName);
                
                // Si c'est une archive, analyser son contenu
                if (isArchive)
                {
                    await ScanArchiveContentsAsync(fileStream, scan);
                }
                else
                {
                    // Pour les fichiers non-archives, effectuer un scan de base
                    await ScanSimpleFileAsync(fileStream, fileName, scan);
                }
                
                // Restaurer la position du stream
                fileStream.Position = originalPosition;
                
                // Mettre à jour le statut final du scan
                scan.Status = scan.Threats.Any() ? ScanStatus.Infected : ScanStatus.Clean;
                scan.IsComplete = true;
                
                _logger.LogInformation(
                    "Scan terminé pour {FileName}. Statut: {Status}, Menaces détectées: {ThreatCount}", 
                    fileName, scan.Status, scan.Threats.Count);
                
                return scan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du scan du fichier {FileName}", fileName);
                
                return new SecurityScan
                {
                    ScannedAt = DateTime.UtcNow,
                    Status = ScanStatus.Failed,
                    IsComplete = true,
                    Comments = $"Erreur lors du scan: {ex.Message}",
                    ScanEngine = "ModHub Security Scanner",
                    ScanEngineVersion = "1.0.0"
                };
            }
        }

        /// <inheritdoc />
        public async Task<SecurityScan> ScanStoredFileAsync(string filePath, string containerName)
        {
            try
            {
                _logger.LogInformation("Démarrage du scan pour le fichier stocké {FilePath} dans {Container}", filePath, containerName);
                
                // Vérifier si le fichier existe
                if (!await _blobStorageService.FileExistsAsync(filePath, containerName))
                {
                    throw new FileNotFoundException($"Le fichier {filePath} n'existe pas dans {containerName}");
                }
                
                // Télécharger le fichier
                using var fileStream = await _blobStorageService.DownloadFileAsync(filePath, containerName);
                
                // Extraire le nom du fichier du chemin
                var fileName = Path.GetFileName(filePath);
                
                // Effectuer le scan
                return await ScanFileAsync(fileStream, fileName, "application/octet-stream");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du scan du fichier stocké {FilePath}", filePath);
                
                return new SecurityScan
                {
                    ScannedAt = DateTime.UtcNow,
                    Status = ScanStatus.Failed,
                    IsComplete = true,
                    Comments = $"Erreur lors du scan du fichier stocké: {ex.Message}",
                    ScanEngine = "ModHub Security Scanner",
                    ScanEngineVersion = "1.0.0"
                };
            }
        }

        /// <inheritdoc />
        public Task<bool> IsScanRequiredAsync(ModFile modFile)
        {
            // Vérifier si le fichier a déjà été scanné et approuvé
            if (modFile.SecurityScan != null && 
                modFile.SecurityScan.IsComplete && 
                modFile.SecurityScan.Status == ScanStatus.Clean &&
                modFile.IsApproved)
            {
                return Task.FromResult(false);
            }
            
            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public bool IsFileSafe(SecurityScan scan)
        {
            // Un fichier est considéré comme sûr si:
            // 1. Le scan est complet
            // 2. Le statut est "Clean" ou le statut est "Infected" mais toutes les menaces sont de faible priorité
            
            if (scan == null || !scan.IsComplete)
            {
                return false;
            }
            
            if (scan.Status == ScanStatus.Clean)
            {
                return true;
            }
            
            if (scan.Status == ScanStatus.Infected)
            {
                // Vérifier si toutes les menaces sont de faible priorité
                return scan.Threats.All(t => t.Severity == SeverityLevel.Low);
            }
            
            return false;
        }
        
        /// <summary>
        /// Analyse le contenu d'une archive
        /// </summary>
        private async Task ScanArchiveContentsAsync(Stream archiveStream, SecurityScan scan)
        {
            try
            {
                // Utiliser SharpCompress pour ouvrir l'archive
                using var archive = ArchiveFactory.Open(archiveStream, new ReaderOptions { LeaveStreamOpen = true });
                
                // Récupérer la structure des dossiers et le nombre total de fichiers
                var folderStructure = new Dictionary<string, int>();
                int totalFiles = 0;
                var detectedFileTypes = new HashSet<string>();
                
                foreach (var entry in archive.Entries)
                {
                    totalFiles++;
                    
                    // Ignorer les répertoires
                    if (entry.IsDirectory)
                    {
                        continue;
                    }
                    
                    // Comptabiliser les types de fichiers
                    string ext = Path.GetExtension(entry.Key).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(ext))
                    {
                        detectedFileTypes.Add(ext);
                    }
                    
                    // Analyser la structure des dossiers
                    string directory = Path.GetDirectoryName(entry.Key);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        if (folderStructure.ContainsKey(directory))
                        {
                            folderStructure[directory]++;
                        }
                        else
                        {
                            folderStructure[directory] = 1;
                        }
                    }
                    
                    // Vérifier les extensions suspectes
                    if (HasSuspiciousExtension(entry.Key))
                    {
                        scan.Threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.Suspicious,
                            Severity = SeverityLevel.Medium,
                            FilePath = entry.Key,
                            ThreatName = "Suspicious File Extension",
                            Description = $"Le fichier '{entry.Key}' a une extension potentiellement dangereuse"
                        });
                        
                        continue;
                    }
                    
                    // Pour les fichiers texte, scripts et autres fichiers qui peuvent être analysés,
                    // extraire le contenu et chercher des patterns suspects
                    if (ShouldInspectFileContent(entry.Key))
                    {
                        try
                        {
                            using var entryStream = entry.OpenEntryStream();
                            using var reader = new StreamReader(entryStream);
                            
                            // Lire jusqu'à 1 Mo du fichier pour l'analyse (éviter les grands fichiers)
                            char[] buffer = new char[Math.Min(1024 * 1024, entryStream.Length)];
                            await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                            string content = new string(buffer);
                            
                            // Vérifier les patterns suspects
                            foreach (var (pattern, description) in SuspiciousPatterns)
                            {
                                if (Regex.IsMatch(content, pattern))
                                {
                                    scan.Threats.Add(new SecurityThreat
                                    {
                                        Type = ThreatType.Suspicious,
                                        Severity = SeverityLevel.High,
                                        FilePath = entry.Key,
                                        ThreatName = "Suspicious Code Pattern",
                                        Description = $"{description} dans '{entry.Key}'"
                                    });
                                    
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Impossible d'analyser le contenu du fichier {FilePath}", entry.Key);
                        }
                    }
                }
                
                // Mettre à jour les informations du scan
                scan.FilesScanned = totalFiles;
                scan.FolderStructure = folderStructure;
                scan.DetectedFileTypes = detectedFileTypes.ToList();
                
                // Tout fichier caché ou sans extension est suspect
                var hiddenFiles = archive.Entries
                    .Where(e => !e.IsDirectory && (Path.GetFileName(e.Key).StartsWith(".") || string.IsNullOrEmpty(Path.GetExtension(e.Key))))
                    .ToList();
                
                if (hiddenFiles.Any())
                {
                    foreach (var hiddenFile in hiddenFiles)
                    {
                        scan.Threats.Add(new SecurityThreat
                        {
                            Type = ThreatType.Suspicious,
                            Severity = SeverityLevel.Low,
                            FilePath = hiddenFile.Key,
                            ThreatName = "Hidden File",
                            Description = $"Fichier caché ou sans extension: '{hiddenFile.Key}'"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse de l'archive");
                scan.Status = ScanStatus.Failed;
                scan.Comments = $"Erreur lors de l'analyse de l'archive: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Analyse un fichier non-archive
        /// </summary>
        private async Task ScanSimpleFileAsync(Stream fileStream, string fileName, SecurityScan scan)
        {
            try
            {
                // Pour un fichier simple, vérifier d'abord l'extension
                if (HasSuspiciousExtension(fileName))
                {
                    scan.Threats.Add(new SecurityThreat
                    {
                        Type = ThreatType.Unauthorized,
                        Severity = SeverityLevel.High,
                        FilePath = fileName,
                        ThreatName = "Unauthorized File Type",
                        Description = $"Le type de fichier '{Path.GetExtension(fileName)}' n'est pas autorisé"
                    });
                }
                
                // Pour les fichiers texte, chercher des patterns suspects
                if (ShouldInspectFileContent(fileName))
                {
                    using var reader = new StreamReader(fileStream);
                    
                    // Lire jusqu'à 1 Mo du fichier pour l'analyse
                    char[] buffer = new char[Math.Min(1024 * 1024, fileStream.Length)];
                    await reader.ReadBlockAsync(buffer, 0, buffer.Length);
                    string content = new string(buffer);
                    
                    foreach (var (pattern, description) in SuspiciousPatterns)
                    {
                        if (Regex.IsMatch(content, pattern))
                        {
                            scan.Threats.Add(new SecurityThreat
                            {
                                Type = ThreatType.Suspicious,
                                Severity = SeverityLevel.High,
                                FilePath = fileName,
                                ThreatName = "Suspicious Content",
                                Description = description
                            });
                            
                            break;
                        }
                    }
                }
                
                // Mettre à jour les informations du scan
                scan.FilesScanned = 1;
                scan.DetectedFileTypes = new List<string> { Path.GetExtension(fileName).ToLowerInvariant() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'analyse du fichier simple {FileName}", fileName);
                scan.Status = ScanStatus.Failed;
                scan.Comments = $"Erreur lors de l'analyse du fichier: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Vérifie si un fichier a une extension considérée comme suspecte
        /// </summary>
        private bool HasSuspiciousExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SuspiciousExtensions.Contains(extension);
        }
        
        /// <summary>
        /// Détermine si le contenu d'un fichier doit être inspecté
        /// </summary>
        private bool ShouldInspectFileContent(string filePath)
        {
            // Extensions de fichiers texte ou scripts qui doivent être analysés
            string[] inspectExtensions = new[] 
            { 
                ".txt", ".ini", ".cfg", ".xml", ".json", ".html", ".htm", ".css", ".js", ".lua", 
                ".py", ".ps1", ".bat", ".cmd", ".sh", ".vbs", ".php", ".asp", ".aspx", ".jsp"
            };
            
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return inspectExtensions.Contains(extension);
        }
        
        /// <summary>
        /// Détermine si le fichier est une archive
        /// </summary>
        private bool IsArchiveFile(string fileName)
        {
            string[] archiveExtensions = new[] { ".zip", ".rar", ".7z" };
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return archiveExtensions.Contains(extension);
        }
    }
}

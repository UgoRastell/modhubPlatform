using Microsoft.Extensions.Logging;
using ModsService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModsService.Services.Versioning
{
    /// <summary>
    /// Service pour maintenir automatiquement l'historique des versions d'un mod
    /// </summary>
    public class VersionHistoryService : IVersionHistoryService
    {
        private readonly ILogger<VersionHistoryService> _logger;
        private readonly IVersioningService _versioningService;
        
        public VersionHistoryService(
            ILogger<VersionHistoryService> logger,
            IVersioningService versioningService)
        {
            _logger = logger;
            _versioningService = versioningService;
        }
        
        /// <inheritdoc />
        public async Task<Mod> UpdateVersionHistoryAsync(Mod mod)
        {
            if (mod == null)
            {
                throw new ArgumentNullException(nameof(mod));
            }
            
            try
            {
                // Mettre à jour les statistiques de l'historique des versions
                mod.VersionHistory.UpdateStats(mod.Versions);
                
                // Mettre à jour le changelog global
                mod.GlobalChangelog = mod.CompileGlobalChangelog();
                
                // Déterminer la version initiale (la plus ancienne)
                if (mod.Versions.Any())
                {
                    var oldestVersion = mod.Versions.OrderBy(v => v.ReleasedAt).FirstOrDefault();
                    if (oldestVersion != null)
                    {
                        mod.InitialVersion = oldestVersion.VersionNumber;
                        mod.FirstReleasedAt = oldestVersion.ReleasedAt;
                    }
                }
                
                // Déterminer la dernière version stable
                var latestStable = mod.Versions
                    .Where(v => v.VersionType == VersionType.Stable && !v.IsHidden)
                    .OrderByDescending(v => v.ReleasedAt)
                    .FirstOrDefault();
                    
                if (latestStable != null)
                {
                    mod.LastStableReleaseAt = latestStable.ReleasedAt;
                    
                    // Si aucune version n'est recommandée explicitement, la dernière version stable devient la recommandée
                    if (!mod.Versions.Any(v => v.IsRecommended))
                    {
                        latestStable.IsRecommended = true;
                        mod.RecommendedVersion = latestStable.VersionNumber;
                    }
                }
                
                // Déterminer la version recommandée (si explicitement recommandée)
                var recommendedVersion = mod.Versions.FirstOrDefault(v => v.IsRecommended);
                if (recommendedVersion != null)
                {
                    mod.RecommendedVersion = recommendedVersion.VersionNumber;
                }
                
                // Déterminer la dernière version en développement
                var latestDev = mod.Versions
                    .Where(v => v.VersionType == VersionType.Development || v.VersionType == VersionType.Alpha || v.VersionType == VersionType.Beta)
                    .OrderByDescending(v => v.ReleasedAt)
                    .FirstOrDefault();
                    
                if (latestDev != null)
                {
                    mod.LatestDevelopmentVersion = latestDev.VersionNumber;
                }
                
                // Vérifier et mettre à jour la CurrentVersion
                if (string.IsNullOrEmpty(mod.CurrentVersion) || !mod.HasVersion(mod.CurrentVersion))
                {
                    // Utiliser la version recommandée si elle existe
                    if (!string.IsNullOrEmpty(mod.RecommendedVersion))
                    {
                        mod.CurrentVersion = mod.RecommendedVersion;
                    }
                    // Sinon utiliser la dernière version stable
                    else if (latestStable != null)
                    {
                        mod.CurrentVersion = latestStable.VersionNumber;
                    }
                    // En dernier recours, utiliser la dernière version
                    else if (mod.Versions.Any())
                    {
                        mod.CurrentVersion = mod.Versions.OrderByDescending(v => v.ReleasedAt).First().VersionNumber;
                    }
                }
                
                // Recalculer les statistiques de téléchargement
                RecalculateDownloadStats(mod);
                
                // Construire les relations de parenté entre les versions
                await BuildVersionsRelationshipsAsync(mod);
                
                return mod;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'historique des versions pour le mod {ModId}", mod.Id);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<string> GetFormattedVersionHistoryAsync(Mod mod, string format = "markdown")
        {
            if (mod == null)
            {
                throw new ArgumentNullException(nameof(mod));
            }
            
            try
            {
                // Formater selon le type demandé
                switch (format.ToLowerInvariant())
                {
                    case "markdown":
                    default:
                        // Compiler ou utiliser le changelog global existant
                        if (string.IsNullOrEmpty(mod.GlobalChangelog))
                        {
                            return mod.CompileGlobalChangelog();
                        }
                        return mod.GlobalChangelog;
                        
                    case "html":
                        // Convertir le markdown en HTML (dans une implémentation plus complète)
                        return $"<h1>{mod.Name} - Historique des versions</h1>" +
                               "<p>Exportation HTML de l'historique non encore implémentée.</p>";
                        
                    case "json":
                        // Dans une implémentation réelle, on utiliserait System.Text.Json pour sérialiser
                        return "{ \"historyFormat\": \"json\", \"notImplemented\": true }";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de l'historique formaté pour le mod {ModId}", mod.Id);
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<DiffResult> CompareTwoVersionsAsync(Mod mod, string version1, string version2)
        {
            if (mod == null)
            {
                throw new ArgumentNullException(nameof(mod));
            }
            
            // Vérifier que les versions existent
            var v1 = mod.GetVersion(version1);
            var v2 = mod.GetVersion(version2);
            
            if (v1 == null)
            {
                throw new KeyNotFoundException($"Version {version1} non trouvée dans le mod {mod.Name}");
            }
            
            if (v2 == null)
            {
                throw new KeyNotFoundException($"Version {version2} non trouvée dans le mod {mod.Name}");
            }
            
            // Déterminer quelle version est la plus récente
            bool v1IsNewer = _versioningService.IsNewerVersion(version1, version2);
            var newerVersion = v1IsNewer ? v1 : v2;
            var olderVersion = v1IsNewer ? v2 : v1;
            
            // Construire la comparaison
            var result = new DiffResult
            {
                OlderVersion = olderVersion.VersionNumber,
                NewerVersion = newerVersion.VersionNumber,
                OlderReleaseDate = olderVersion.ReleasedAt,
                NewerReleaseDate = newerVersion.ReleasedAt,
                TimeSpanInDays = (int)(newerVersion.ReleasedAt - olderVersion.ReleasedAt).TotalDays,
                ChangedFiles = new List<string>(),
                AddedFiles = new List<string>(),
                RemovedFiles = new List<string>(),
                SizeChangeDelta = newerVersion.TotalSizeBytes - olderVersion.TotalSizeBytes
            };
            
            // Comparer les fichiers entre les versions
            var olderFiles = olderVersion.Files.Select(f => f.FileName).ToHashSet();
            var newerFiles = newerVersion.Files.Select(f => f.FileName).ToHashSet();
            
            result.AddedFiles.AddRange(newerFiles.Except(olderFiles));
            result.RemovedFiles.AddRange(olderFiles.Except(newerFiles));
            
            // Pour les fichiers modifiés, comparer les hachages
            foreach (var fileName in olderFiles.Intersect(newerFiles))
            {
                var olderFile = olderVersion.Files.First(f => f.FileName == fileName);
                var newerFile = newerVersion.Files.First(f => f.FileName == fileName);
                
                if (olderFile.Md5Hash != newerFile.Md5Hash)
                {
                    result.ChangedFiles.Add(fileName);
                }
            }
            
            // Récupérer les autres versions entre les deux
            var allVersions = mod.Versions
                .OrderBy(v => v.ReleasedAt)
                .ToList();
                
            var olderIndex = allVersions.FindIndex(v => v.VersionNumber == olderVersion.VersionNumber);
            var newerIndex = allVersions.FindIndex(v => v.VersionNumber == newerVersion.VersionNumber);
            
            if (olderIndex >= 0 && newerIndex >= 0)
            {
                result.IntermediateVersions = allVersions
                    .Skip(Math.Min(olderIndex, newerIndex) + 1)
                    .Take(Math.Abs(newerIndex - olderIndex) - 1)
                    .Select(v => v.VersionNumber)
                    .ToList();
            }
            
            // Construire un changelog combiné entre les deux versions
            result.CombinedChangelog = BuildCombinedChangelog(mod, olderVersion.VersionNumber, newerVersion.VersionNumber);
            
            return result;
        }
        
        /// <summary>
        /// Recalcule les statistiques de téléchargement pour le mod
        /// </summary>
        private void RecalculateDownloadStats(Mod mod)
        {
            // Mettre à jour le total des téléchargements
            mod.DownloadCount = mod.Versions.Sum(v => v.DownloadCount);
            
            // Mettre à jour les téléchargements par version
            mod.DownloadsByVersion = new Dictionary<string, long>();
            foreach (var version in mod.Versions)
            {
                mod.DownloadsByVersion[version.VersionNumber] = version.DownloadCount;
            }
        }
        
        /// <summary>
        /// Construit les relations de parenté entre les versions
        /// </summary>
        private async Task BuildVersionsRelationshipsAsync(Mod mod)
        {
            // Trier les versions par date de sortie
            var orderedVersions = mod.Versions
                .OrderBy(v => v.ReleasedAt)
                .ToList();
                
            // Pour chaque version (sauf la première), établir le lien avec la version précédente
            for (int i = 1; i < orderedVersions.Count; i++)
            {
                var currentVersion = orderedVersions[i];
                var previousVersion = orderedVersions[i - 1];
                
                // Ne mettre à jour que si la référence n'existe pas déjà
                if (string.IsNullOrEmpty(currentVersion.PreviousVersionId) && !string.IsNullOrEmpty(previousVersion.Id))
                {
                    currentVersion.PreviousVersionId = previousVersion.Id;
                }
            }
        }
        
        /// <summary>
        /// Construit un changelog combiné entre deux versions
        /// </summary>
        private string BuildCombinedChangelog(Mod mod, string olderVersion, string newerVersion)
        {
            var versions = mod.Versions
                .OrderBy(v => v.ReleasedAt)
                .ToList();
                
            var olderIndex = versions.FindIndex(v => v.VersionNumber == olderVersion);
            var newerIndex = versions.FindIndex(v => v.VersionNumber == newerVersion);
            
            if (olderIndex < 0 || newerIndex < 0)
            {
                return "Impossible de générer le changelog combiné : versions introuvables";
            }
            
            // Assurer que olderIndex < newerIndex
            if (olderIndex > newerIndex)
            {
                var temp = olderIndex;
                olderIndex = newerIndex;
                newerIndex = temp;
            }
            
            var combinedChangelog = new System.Text.StringBuilder();
            combinedChangelog.AppendLine($"# Changements entre {olderVersion} et {newerVersion}");
            combinedChangelog.AppendLine();
            
            // Fusionner les changelogs structurés
            var allAdded = new List<string>();
            var allChanged = new List<string>();
            var allRemoved = new List<string>();
            var allFixed = new List<string>();
            var allSecurity = new List<string>();
            var allPerformance = new List<string>();
            var allOther = new List<string>();
            
            for (int i = olderIndex + 1; i <= newerIndex; i++)
            {
                var version = versions[i];
                
                if (version.StructuredChangelog != null)
                {
                    allAdded.AddRange(version.StructuredChangelog.Added);
                    allChanged.AddRange(version.StructuredChangelog.Changed);
                    allRemoved.AddRange(version.StructuredChangelog.Removed);
                    allFixed.AddRange(version.StructuredChangelog.Fixed);
                    allSecurity.AddRange(version.StructuredChangelog.Security);
                    allPerformance.AddRange(version.StructuredChangelog.Performance);
                    allOther.AddRange(version.StructuredChangelog.Other);
                }
            }
            
            // Construire le changelog combiné
            if (allAdded.Count > 0)
            {
                combinedChangelog.AppendLine("## ✨ Nouveautés");
                foreach (var item in allAdded)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allChanged.Count > 0)
            {
                combinedChangelog.AppendLine("## 🔄 Changements");
                foreach (var item in allChanged)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allFixed.Count > 0)
            {
                combinedChangelog.AppendLine("## 🐛 Corrections");
                foreach (var item in allFixed)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allRemoved.Count > 0)
            {
                combinedChangelog.AppendLine("## 🗑️ Suppressions");
                foreach (var item in allRemoved)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allSecurity.Count > 0)
            {
                combinedChangelog.AppendLine("## 🔒 Sécurité");
                foreach (var item in allSecurity)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allPerformance.Count > 0)
            {
                combinedChangelog.AppendLine("## ⚡ Performance");
                foreach (var item in allPerformance)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            if (allOther.Count > 0)
            {
                combinedChangelog.AppendLine("## 📝 Autres");
                foreach (var item in allOther)
                {
                    combinedChangelog.AppendLine($"- {item}");
                }
                combinedChangelog.AppendLine();
            }
            
            return combinedChangelog.ToString();
        }
    }
    
    /// <summary>
    /// Résultat d'une comparaison entre deux versions
    /// </summary>
    public class DiffResult
    {
        /// <summary>
        /// Version la plus ancienne
        /// </summary>
        public string OlderVersion { get; set; }
        
        /// <summary>
        /// Version la plus récente
        /// </summary>
        public string NewerVersion { get; set; }
        
        /// <summary>
        /// Date de sortie de la version la plus ancienne
        /// </summary>
        public DateTime OlderReleaseDate { get; set; }
        
        /// <summary>
        /// Date de sortie de la version la plus récente
        /// </summary>
        public DateTime NewerReleaseDate { get; set; }
        
        /// <summary>
        /// Durée en jours entre les deux versions
        /// </summary>
        public int TimeSpanInDays { get; set; }
        
        /// <summary>
        /// Liste des versions intermédiaires entre les deux versions comparées
        /// </summary>
        public List<string> IntermediateVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des fichiers ajoutés
        /// </summary>
        public List<string> AddedFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des fichiers modifiés
        /// </summary>
        public List<string> ChangedFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des fichiers supprimés
        /// </summary>
        public List<string> RemovedFiles { get; set; } = new List<string>();
        
        /// <summary>
        /// Différence de taille en octets entre les deux versions
        /// </summary>
        public long SizeChangeDelta { get; set; }
        
        /// <summary>
        /// Changelog combiné entre les deux versions
        /// </summary>
        public string CombinedChangelog { get; set; }
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModsService.Models
{
    /// <summary>
    /// Représente un mod complet avec toutes ses versions et métadonnées
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Mod : BaseModel
    {
        /// <summary>
        /// Nom du mod
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant unique pour l'URL du mod (slug)
        /// </summary>
        public string Slug { get; set; } = string.Empty;
        
        /// <summary>
        /// Description détaillée du mod en format markdown
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Description courte pour les aperçus
        /// </summary>
        public string ShortDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur créateur
        /// </summary>
        public string CreatorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'affichage du créateur
        /// </summary>
        public string CreatorName { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du jeu auquel ce mod est associé
        /// </summary>
        public string GameId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du jeu
        /// </summary>
        public string GameName { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la catégorie principale
        /// </summary>
        public string CategoryId { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de l'image de miniature
        /// </summary>
        public string ThumbnailUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Liste des URLs des captures d'écran
        /// </summary>
        public List<string> ScreenshotUrls { get; set; } = new List<string>();
        
        /// <summary>
        /// Nombre total de téléchargements toutes versions confondues
        /// </summary>
        public long DownloadCount { get; set; } = 0;
        
        /// <summary>
        /// Liste des évaluations
        /// </summary>
        public List<Rating> Ratings { get; set; } = new List<Rating>();
        
        /// <summary>
        /// Note moyenne des évaluations
        /// </summary>
        public double Rating { get; set; } = 0;
        
        /// <summary>
        /// Nombre d'évaluations
        /// </summary>
        public int RatingCount { get; set; } = 0;
        
        /// <summary>
        /// Liste des tags associés au mod
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des catégories secondaires
        /// </summary>
        public List<string> Categories { get; set; } = new List<string>();
        
        /// <summary>
        /// Indique si le mod est mis en avant
        /// </summary>
        public bool IsFeatured { get; set; } = false;
        
        /// <summary>
        /// Indique si le mod a été approuvé par un modérateur
        /// </summary>
        public bool IsApproved { get; set; } = false;
        
        /// <summary>
        /// Date de dernière modification importante (metadata, nouvelle version, etc.)
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// URL de la documentation externe (site web du mod, wiki, etc.)
        /// </summary>
        public string DocumentationUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Version actuelle du mod (la plus récente stable ou recommandée)
        /// </summary>
        public string CurrentVersion { get; set; } = "1.0.0";
        
        /// <summary>
        /// Numéro de la dernière version en développement
        /// </summary>
        public string LatestDevelopmentVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Version recommandée par le créateur
        /// </summary>
        public string RecommendedVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Version initiale/première version du mod
        /// </summary>
        public string InitialVersion { get; set; } = "1.0.0";
        
        /// <summary>
        /// Date de la première release
        /// </summary>
        public DateTime FirstReleasedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de la dernière release stable
        /// </summary>
        public DateTime LastStableReleaseAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Liste de toutes les versions du mod, de la plus récente à la plus ancienne
        /// </summary>
        public List<ModVersion> Versions { get; set; } = new List<ModVersion>();
        
        /// <summary>
        /// Méta-informations sur l'historique des versions (pour affichage rapide)
        /// </summary>
        public VersionHistory VersionHistory { get; set; } = new VersionHistory();
        
        /// <summary>
        /// Changelog global compilant les changements majeurs
        /// </summary>
        public string GlobalChangelog { get; set; } = string.Empty;
        
        /// <summary>
        /// Statistiques des téléchargements par version
        /// </summary>
        public Dictionary<string, long> DownloadsByVersion { get; set; } = new Dictionary<string, long>();
        
        /// <summary>
        /// Quota de téléchargement global pour ce mod (0 = illimité)
        /// </summary>
        public long DownloadQuota { get; set; } = 0;
        
        /// <summary>
        /// Taille totale de stockage utilisée par toutes les versions du mod (en bytes)
        /// </summary>
        public long TotalStorageSize { get; set; } = 0;
        
        /// <summary>
        /// URL de la documentation externe (site web du mod, wiki, etc.)
        /// </summary>
        public string IssueTrackerUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// URL du dépôt de code source
        /// </summary>
        public string SourceCodeUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du jeu pour lequel ce mod est un port/adaptation
        /// </summary>
        public string PortedFromGameId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID d'un mod dont celui-ci est un fork/adaptation
        /// </summary>
        public string ForkedFromModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Liste des ID de mods qui constituent des dépendances requises
        /// </summary>
        public List<string> RequiredDependencies { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des ID de mods qui constituent des dépendances optionnelles
        /// </summary>
        public List<string> OptionalDependencies { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des ID de mods avec lesquels celui-ci est incompatible
        /// </summary>
        public List<string> IncompatibleMods { get; set; } = new List<string>();
        
        /// <summary>
        /// Indicateur si le mod a été signalé pour contenu inapproprié
        /// </summary>
        public bool IsFlagged { get; set; } = false;
        
        /// <summary>
        /// Raison du signalement si applicable
        /// </summary>
        public string FlagReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Retourne la version spécifiée par son numéro
        /// </summary>
        /// <param name="versionNumber">Numéro de version recherché</param>
        /// <returns>La version si elle existe, null sinon</returns>
        public ModVersion GetVersion(string versionNumber)
        {
            return Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
        }
        
        /// <summary>
        /// Vérifie si une version spécifique existe
        /// </summary>
        /// <param name="versionNumber">Numéro de version à vérifier</param>
        /// <returns>True si la version existe</returns>
        public bool HasVersion(string versionNumber)
        {
            return Versions.Any(v => v.VersionNumber == versionNumber);
        }
        
        /// <summary>
        /// Compile un changelog global à partir de toutes les versions
        /// </summary>
        /// <returns>Changelog global au format Markdown</returns>
        public string CompileGlobalChangelog()
        {
            var changelog = new System.Text.StringBuilder();
            changelog.AppendLine($"# {Name} - Historique des versions");
            changelog.AppendLine();
            
            // Ajouter des informations globales
            if (!string.IsNullOrEmpty(ShortDescription))
            {
                changelog.AppendLine($"> {ShortDescription}");
                changelog.AppendLine();
            }
            
            changelog.AppendLine($"De la version {InitialVersion} (sortie le {FirstReleasedAt:dd/MM/yyyy}) à la version {CurrentVersion} (mise à jour le {LastStableReleaseAt:dd/MM/yyyy}).");
            changelog.AppendLine();
            
            // Ajouter les versions du plus récent au plus ancien
            foreach (var version in Versions.OrderByDescending(v => v.ReleasedAt))
            {
                changelog.AppendLine($"## Version {version.VersionNumber} - {version.ReleasedAt:dd/MM/yyyy}");
                
                if (version.VersionNumber == CurrentVersion)
                    changelog.AppendLine("**Version actuelle**");
                    
                if (version.VersionNumber == RecommendedVersion)
                    changelog.AppendLine("**Version recommandée**");
                
                if (!string.IsNullOrEmpty(version.Changelog))
                {
                    changelog.AppendLine(version.Changelog);
                }
                else if (version.StructuredChangelog != null)
                {
                    // Si pas de changelog standard mais un structuré, utiliser celui-ci
                    changelog.AppendLine(version.StructuredChangelog.ToMarkdown());
                }
                else
                {
                    changelog.AppendLine("*Aucune note de version fournie.*");
                }
                
                changelog.AppendLine();
            }
            
            return changelog.ToString();
        }
    }
    
    /// <summary>
    /// Classe pour stocker des informations rapides sur l'historique des versions
    /// </summary>
    [BsonIgnoreExtraElements]
    public class VersionHistory
    {        
        /// <summary>
        /// Nombre total de versions
        /// </summary>
        public int TotalVersions { get; set; } = 0;
        
        /// <summary>
        /// Nombre de versions stables
        /// </summary>
        public int StableVersions { get; set; } = 0;
        
        /// <summary>
        /// Nombre de versions beta
        /// </summary>
        public int BetaVersions { get; set; } = 0;
        
        /// <summary>
        /// Nombre de versions alpha
        /// </summary>
        public int AlphaVersions { get; set; } = 0;
        
        /// <summary>
        /// Liste des 5 dernières versions (numéros de version)
        /// </summary>
        public List<VersionSummary> RecentVersions { get; set; } = new List<VersionSummary>();
        
        /// <summary>
        /// Date de la dernière mise à jour majeure
        /// </summary>
        public DateTime LastMajorUpdateAt { get; set; } = DateTime.MinValue;
        
        /// <summary>
        /// Liste des versions majeures avec leurs dates de sortie
        /// </summary>
        public List<VersionSummary> MajorVersions { get; set; } = new List<VersionSummary>();
        
        /// <summary>
        /// Met à jour les statistiques d'historique des versions
        /// </summary>
        /// <param name="versions">Liste des versions à analyser</param>
        public void UpdateStats(List<ModVersion> versions)
        {            
            TotalVersions = versions.Count;
            StableVersions = versions.Count(v => v.VersionType == VersionType.Stable);
            BetaVersions = versions.Count(v => v.VersionType == VersionType.Beta);
            AlphaVersions = versions.Count(v => v.VersionType == VersionType.Alpha);
            
            // Récupérer les 5 dernières versions
            RecentVersions = versions
                .OrderByDescending(v => v.ReleasedAt)
                .Take(5)
                .Select(v => new VersionSummary
                {
                    Version = v.VersionNumber,
                    ReleasedAt = v.ReleasedAt,
                    Type = v.VersionType
                })
                .ToList();
            
            // Trouver les versions majeures (1.0.0, 2.0.0, etc.)
            MajorVersions = versions
                .Where(v => v.VersionNumber.EndsWith(".0.0"))
                .OrderByDescending(v => v.ReleasedAt)
                .Select(v => new VersionSummary
                {
                    Version = v.VersionNumber,
                    ReleasedAt = v.ReleasedAt,
                    Type = v.VersionType
                })
                .ToList();
                
            // Si des versions majeures existent, mettre à jour la date de dernière mise à jour majeure
            if (MajorVersions.Any())
            {
                LastMajorUpdateAt = MajorVersions.First().ReleasedAt;
            }
        }
    }
    
    /// <summary>
    /// Résumé d'une version pour l'historique rapide
    /// </summary>
    [BsonIgnoreExtraElements]
    public class VersionSummary
    {
        /// <summary>
        /// Numéro de version
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de sortie
        /// </summary>
        public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Type de version
        /// </summary>
        public VersionType Type { get; set; } = VersionType.Stable;
    }
}

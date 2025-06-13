using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

namespace ModsService.Models
{
    /// <summary>
    /// Représente une version spécifique d'un mod avec ses fichiers associés
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ModVersion
    {
        /// <summary>
        /// Identifiant unique de la version
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        /// <summary>
        /// Numéro de version unique (ex: "1.0.0", "2.3.1-beta", etc.)
        /// </summary>
        public string VersionNumber { get; set; } = string.Empty;
        
        /// <summary>
        /// Notes de version / Changelog en format markdown complet
        /// </summary>
        public string Changelog { get; set; } = string.Empty;
        
        /// <summary>
        /// Changelog structuré par catégories (Ajouts, Corrections, etc.)
        /// </summary>
        public StructuredChangelog StructuredChangelog { get; set; } = new StructuredChangelog();
        
        /// <summary>
        /// Référence à la version précédente (pour la traçabilité)
        /// </summary>
        public string PreviousVersionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Liste des versions avec lesquelles cette version est compatible
        /// </summary>
        public List<string> CompatibleVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des versions avec lesquelles cette version est incompatible
        /// </summary>
        public List<string> IncompatibleVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Identifie l'auteur de cette version (peut être différent du créateur du mod)
        /// </summary>
        public string AuthorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom de l'auteur de cette version
        /// </summary>
        public string AuthorName { get; set; } = string.Empty;
        
        /// <summary>
        /// Requis minimums pour utiliser cette version (ex: "Jeu v1.2+", "Mod X v2.0+", etc.)
        /// </summary>
        public List<string> Requirements { get; set; } = new List<string>();
        
        /// <summary>
        /// Date de publication de cette version
        /// </summary>
        public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Liste des fichiers associés à cette version
        /// </summary>
        public List<ModFile> Files { get; set; } = new List<ModFile>();
        
        /// <summary>
        /// Nombre de téléchargements pour cette version spécifique
        /// </summary>
        public long DownloadCount { get; set; } = 0;
        
        /// <summary>
        /// Taille totale des fichiers de cette version (en bytes)
        /// </summary>
        public long TotalSizeBytes { get; set; } = 0;
        
        /// <summary>
        /// Type de version (stable, beta, alpha, etc.)
        /// </summary>
        public VersionType VersionType { get; set; } = VersionType.Stable;
        
        /// <summary>
        /// Indique si cette version est la version recommandée
        /// </summary>
        public bool IsRecommended { get; set; } = false;
        
        /// <summary>
        /// Indique si cette version a été cachée (non accessible aux utilisateurs standard)
        /// </summary>
        public bool IsHidden { get; set; } = false;
        
        /// <summary>
        /// Liste des versions compatibles du jeu
        /// </summary>
        public List<string> CompatibleGameVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// URL d'un éventuel fichier source (ex. GitHub) pour cette version
        /// </summary>
        public string SourceCodeUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Hash du commit ou identifiant de version source si applicable
        /// </summary>
        public string SourceVersionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Métadonnées spécifiques à cette version (serialisées en JSON)
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Notes internes pour les modérateurs/administrateurs (non visible au public)
        /// </summary>
        public string InternalNotes { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de dernière modification de cette version
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Indique si cette version a été automatiquement approuvée
        /// </summary>
        public bool IsAutoApproved { get; set; } = false;
        
        /// <summary>
        /// Indique si cette version a été retirée/dépréciée
        /// </summary>
        public bool IsRetired { get; set; } = false;
        
        /// <summary>
        /// Raison du retrait/dépréciation si applicable
        /// </summary>
        public string RetirementReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de retrait/dépréciation si applicable
        /// </summary>
        public DateTime? RetiredAt { get; set; } = null;
    }

    /// <summary>
    /// Types de version d'un mod
    /// </summary>
    public enum VersionType
    {
        /// <summary>
        /// Version stable pour une utilisation générale
        /// </summary>
        Stable,
        
        /// <summary>
        /// Version beta pour tests avancés
        /// </summary>
        Beta,
        
        /// <summary>
        /// Version alpha pour tests précoces
        /// </summary>
        Alpha,
        
        /// <summary>
        /// Version de développement, potentiellement instable
        /// </summary>
        Development,
        
        /// <summary>
        /// Version obsolète remplacée par des versions plus récentes
        /// </summary>
        Deprecated,
        
        /// <summary>
        /// Version de hotfix pour corriger un problème critique
        /// </summary>
        Hotfix,
        
        /// <summary>
        /// Version release candidate, presque prête pour la production
        /// </summary>
        ReleaseCandidate
    }
    
    /// <summary>
    /// Représente un changelog structuré par catégories
    /// </summary>
    [BsonIgnoreExtraElements]
    public class StructuredChangelog
    {        
        /// <summary>
        /// Liste des nouvelles fonctionnalités ajoutées
        /// </summary>
        public List<string> Added { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des changements apportés aux fonctionnalités existantes
        /// </summary>
        public List<string> Changed { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des fonctionnalités obsolètes ou supprimées
        /// </summary>
        public List<string> Removed { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des bugs corrigés
        /// </summary>
        public List<string> Fixed { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des problèmes de sécurité corrigés
        /// </summary>
        public List<string> Security { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des optimisations de performance
        /// </summary>
        public List<string> Performance { get; set; } = new List<string>();
        
        /// <summary>
        /// Liste des changements divers ne rentrant pas dans les autres catégories
        /// </summary>
        public List<string> Other { get; set; } = new List<string>();
        
        /// <summary>
        /// Convertit le changelog structuré en format Markdown
        /// </summary>
        /// <returns>Chaîne formatée en Markdown</returns>
        public string ToMarkdown()
        {
            var markdown = new System.Text.StringBuilder();
            
            if (Added.Count > 0)
            {
                markdown.AppendLine("### ✨ Nouveautés");
                foreach (var item in Added)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Changed.Count > 0)
            {
                markdown.AppendLine("### 🔄 Changements");
                foreach (var item in Changed)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Fixed.Count > 0)
            {
                markdown.AppendLine("### 🐛 Corrections");
                foreach (var item in Fixed)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Removed.Count > 0)
            {
                markdown.AppendLine("### 🗑️ Suppressions");
                foreach (var item in Removed)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Security.Count > 0)
            {
                markdown.AppendLine("### 🔒 Sécurité");
                foreach (var item in Security)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Performance.Count > 0)
            {
                markdown.AppendLine("### ⚡ Performance");
                foreach (var item in Performance)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            if (Other.Count > 0)
            {
                markdown.AppendLine("### 📝 Autres");
                foreach (var item in Other)
                {
                    markdown.AppendLine($"- {item}");
                }
                markdown.AppendLine();
            }
            
            return markdown.ToString();
        }
    }
}

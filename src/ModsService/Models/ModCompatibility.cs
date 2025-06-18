using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ModsService.Models
{
    /// <summary>
    /// Représente une relation de compatibilité entre mods
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ModCompatibility
    {
        /// <summary>
        /// Identifiant unique de la relation de compatibilité
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        
        /// <summary>
        /// ID du mod source (celui qui déclare la compatibilité)
        /// </summary>
        public string SourceModId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du mod cible concerné par la relation de compatibilité
        /// </summary>
        public string TargetModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du mod cible (pour affichage)
        /// </summary>
        public string TargetModName { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de relation de compatibilité
        /// </summary>
        public CompatibilityType Type { get; set; } = CompatibilityType.Compatible;
        
        /// <summary>
        /// Versions du mod source concernées par cette relation
        /// </summary>
        public List<string> SourceVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Versions du mod cible concernées par cette relation
        /// </summary>
        public List<string> TargetVersions { get; set; } = new List<string>();
        
        /// <summary>
        /// Notes détaillées sur la compatibilité
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Date à laquelle cette relation a été définie
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de la dernière mise à jour de cette relation
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// URL vers un patch ou une ressource pour améliorer la compatibilité
        /// </summary>
        public string PatchUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// URL vers une documentation expliquant cette compatibilité
        /// </summary>
        public string DocumentationUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Vérificateur par la communauté (ID de l'utilisateur)
        /// </summary>
        public string VerifiedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de vérification par la communauté
        /// </summary>
        public DateTime? VerifiedAt { get; set; } = null;
        
        /// <summary>
        /// Nombre de votes de confirmation de cette compatibilité
        /// </summary>
        public int ConfirmationVotes { get; set; } = 0;
        
        /// <summary>
        /// Nombre de votes contestant cette compatibilité
        /// </summary>
        public int DisputeVotes { get; set; } = 0;
        
        /// <summary>
        /// Vérifie si les versions spécifiées sont concernées par cette relation de compatibilité
        /// </summary>
        /// <param name="sourceVersion">Version du mod source</param>
        /// <param name="targetVersion">Version du mod cible</param>
        /// <returns>True si les versions sont concernées par cette relation</returns>
        public bool AppliesTo(string sourceVersion, string targetVersion)
        {
            // Si aucune version spécifiée, la relation s'applique à toutes les versions
            var appliesToSource = SourceVersions.Count == 0 || SourceVersions.Contains(sourceVersion);
            var appliesToTarget = TargetVersions.Count == 0 || TargetVersions.Contains(targetVersion);
            
            return appliesToSource && appliesToTarget;
        }
        
        /// <summary>
        /// Retourne une description textuelle de la relation de compatibilité
        /// </summary>
        /// <returns>Description de la compatibilité</returns>
        public string GetDescription()
        {
            var sourceVersionText = SourceVersions.Count > 0 ? $"v{string.Join(", v", SourceVersions)}" : "toutes versions";
            var targetVersionText = TargetVersions.Count > 0 ? $"v{string.Join(", v", TargetVersions)}" : "toutes versions";
            
            var description = $"{TargetModName} ({targetVersionText}) est {GetCompatibilityTypeDescription()} avec ce mod ({sourceVersionText})";
            
            if (!string.IsNullOrEmpty(Notes))
            {
                description += $": {Notes}";
            }
            
            return description;
        }
        
        /// <summary>
        /// Obtient une description textuelle du type de compatibilité
        /// </summary>
        private string GetCompatibilityTypeDescription()
        {
            return Type switch
            {
                CompatibilityType.Compatible => "compatible",
                CompatibilityType.PartiallyCompatible => "partiellement compatible",
                CompatibilityType.Incompatible => "incompatible",
                CompatibilityType.RequiresPatch => "compatible avec patch",
                CompatibilityType.Enhances => "optimisé pour fonctionner avec",
                _ => "de compatibilité inconnue"
            };
        }
    }
    
    /// <summary>
    /// Types de relation de compatibilité entre mods
    /// </summary>
    public enum CompatibilityType
    {
        /// <summary>
        /// Totalement compatible
        /// </summary>
        Compatible,
        
        /// <summary>
        /// Partiellement compatible (avec limitations)
        /// </summary>
        PartiallyCompatible,
        
        /// <summary>
        /// Incompatible
        /// </summary>
        Incompatible,
        
        /// <summary>
        /// Requiert un patch ou une configuration spéciale
        /// </summary>
        RequiresPatch,
        
        /// <summary>
        /// Améliore le fonctionnement du mod
        /// </summary>
        Enhances
    }
}

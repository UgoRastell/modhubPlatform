using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    /// <summary>
    /// DTO représentant une relation de compatibilité entre mods
    /// </summary>
    public class ModCompatibilityDto
    {
        /// <summary>
        /// Identifiant unique de la relation de compatibilité
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
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
        public CompatibilityTypeDto Type { get; set; } = CompatibilityTypeDto.Compatible;
        
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
        /// URL vers un patch ou une ressource pour améliorer la compatibilité
        /// </summary>
        public string PatchUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// URL vers une documentation expliquant cette compatibilité
        /// </summary>
        public string DocumentationUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre de votes de confirmation de cette compatibilité
        /// </summary>
        public int ConfirmationVotes { get; set; } = 0;
        
        /// <summary>
        /// Nombre de votes contestant cette compatibilité
        /// </summary>
        public int DisputeVotes { get; set; } = 0;
    }
    
    /// <summary>
    /// Types de relation de compatibilité entre mods
    /// </summary>
    public enum CompatibilityTypeDto
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

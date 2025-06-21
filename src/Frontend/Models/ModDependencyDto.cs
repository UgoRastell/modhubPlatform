using System;

namespace Frontend.Models
{
    /// <summary>
    /// DTO représentant une dépendance entre mods
    /// </summary>
    public class ModDependencyDto
    {
        /// <summary>
        /// ID de la dépendance
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du mod dont dépend cette version
        /// </summary>
        public string DependencyModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du mod dépendant (pour affichage)
        /// </summary>
        public string DependencyModName { get; set; } = string.Empty;
        
        /// <summary>
        /// Version minimale requise
        /// </summary>
        public string MinimumVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Version maximale supportée (optionnel)
        /// </summary>
        public string? MaximumVersion { get; set; } = null;
        
        /// <summary>
        /// Si true, le mod ne fonctionnera pas sans cette dépendance
        /// </summary>
        public bool IsRequired { get; set; } = true;
        
        /// <summary>
        /// Notes additionnelles sur la dépendance
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// URL vers une ressource externe expliquant cette dépendance
        /// </summary>
        public string DocumentationUrl { get; set; } = string.Empty;
    }
}

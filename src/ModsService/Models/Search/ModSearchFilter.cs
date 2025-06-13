using System;
using System.Collections.Generic;

namespace ModsService.Models
{
    /// <summary>
    /// Filtre de recherche avancée pour les mods
    /// </summary>
    public class ModSearchFilter
    {
        /// <summary>
        /// ID du jeu
        /// </summary>
        public string GameId { get; set; }
        
        /// <summary>
        /// ID de la catégorie
        /// </summary>
        public string CategoryId { get; set; }
        
        /// <summary>
        /// ID du créateur
        /// </summary>
        public string CreatorId { get; set; }
        
        /// <summary>
        /// Texte de recherche (nom, description)
        /// </summary>
        public string SearchText { get; set; }
        
        /// <summary>
        /// Tags à rechercher
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Type de correspondance pour les tags (any, all)
        /// </summary>
        public string TagMatchType { get; set; } = "any";
        
        /// <summary>
        /// Version minimale du mod
        /// </summary>
        public string MinVersion { get; set; }
        
        /// <summary>
        /// Note minimale
        /// </summary>
        public double? MinRating { get; set; }
        
        /// <summary>
        /// Nombre minimal de téléchargements
        /// </summary>
        public int? MinDownloads { get; set; }
        
        /// <summary>
        /// Date de création après
        /// </summary>
        public DateTime? CreatedAfter { get; set; }
        
        /// <summary>
        /// Date de mise à jour après
        /// </summary>
        public DateTime? UpdatedAfter { get; set; }
        
        /// <summary>
        /// Version du jeu pour la compatibilité
        /// </summary>
        public string GameVersion { get; set; }
        
        /// <summary>
        /// Uniquement les mods avec des versions stables
        /// </summary>
        public bool? HasStableVersion { get; set; }
        
        /// <summary>
        /// Requiert uniquement les mods recommandés par leur créateur
        /// </summary>
        public bool? IsRecommended { get; set; }
        
        /// <summary>
        /// Inclure seulement les mods qui sont activement maintenus
        /// (mis à jour dans les derniers X mois, par exemple)
        /// </summary>
        public bool? IsActiveMaintained { get; set; }
        
        /// <summary>
        /// Période en mois pour définir un mod comme activement maintenu
        /// </summary>
        public int ActiveMaintenanceMonths { get; set; } = 3;
        
        /// <summary>
        /// Exclure les mods cachés/archivés
        /// </summary>
        public bool ExcludeHidden { get; set; } = true;
    }
}

using System;

namespace ModsService.Models
{
    /// <summary>
    /// Options de tri pour les résultats de recherche de mods
    /// </summary>
    public class ModSortOptions
    {
        /// <summary>
        /// Champ de tri principal
        /// </summary>
        public ModSortField SortField { get; set; } = ModSortField.Downloads;
        
        /// <summary>
        /// Direction du tri
        /// </summary>
        public SortDirection Direction { get; set; } = SortDirection.Descending;
        
        /// <summary>
        /// Champ de tri secondaire (en cas d'égalité sur le tri principal)
        /// </summary>
        public ModSortField? SecondarySortField { get; set; }
        
        /// <summary>
        /// Direction du tri secondaire
        /// </summary>
        public SortDirection SecondaryDirection { get; set; } = SortDirection.Descending;
    }
    
    /// <summary>
    /// Champs sur lesquels on peut trier les mods
    /// </summary>
    public enum ModSortField
    {
        /// <summary>
        /// Tri par nombre de téléchargements
        /// </summary>
        Downloads,
        
        /// <summary>
        /// Tri par note moyenne
        /// </summary>
        Rating,
        
        /// <summary>
        /// Tri par date de création
        /// </summary>
        Created,
        
        /// <summary>
        /// Tri par date de mise à jour
        /// </summary>
        Updated,
        
        /// <summary>
        /// Tri par nom du mod
        /// </summary>
        Name,
        
        /// <summary>
        /// Tri par nombre de versions
        /// </summary>
        VersionCount,
        
        /// <summary>
        /// Tri par date de la dernière version stable
        /// </summary>
        LastStableRelease,
        
        /// <summary>
        /// Tri par popularité récente (basée sur les téléchargements récents)
        /// </summary>
        RecentPopularity,
        
        /// <summary>
        /// Tri par taille (totale ou de la version actuelle)
        /// </summary>
        Size,
        
        /// <summary>
        /// Tri par pertinence (utilisé lors d'une recherche textuelle)
        /// </summary>
        Relevance
    }
    
    /// <summary>
    /// Direction de tri
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Tri ascendant (A-Z, 0-9)
        /// </summary>
        Ascending,
        
        /// <summary>
        /// Tri descendant (Z-A, 9-0)
        /// </summary>
        Descending
    }
}

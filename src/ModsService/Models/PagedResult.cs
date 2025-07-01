using System.Collections.Generic;

namespace ModsService.Models
{
    /// <summary>
    /// Résultat paginé pour les listes
    /// </summary>
    /// <typeparam name="T">Type des éléments</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Les éléments de la page courante
        /// </summary>
        public IEnumerable<T> Items { get; set; } = new List<T>();
        
        /// <summary>
        /// Le nombre total d'éléments
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Le numéro de la page courante (1-based)
        /// </summary>
        public int CurrentPage { get; set; }
        
        /// <summary>
        /// Le nombre d'éléments par page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Le nombre total de pages
        /// </summary>
        public int TotalPages => (int)System.Math.Ceiling(TotalCount / (double)PageSize);
        
        /// <summary>
        /// Indique s'il y a une page précédente
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;
        
        /// <summary>
        /// Indique s'il y a une page suivante
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

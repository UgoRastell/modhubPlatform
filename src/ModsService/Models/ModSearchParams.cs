namespace ModsService.Models
{
    /// <summary>
    /// Paramètres de recherche pour les mods
    /// </summary>
    public class ModSearchParams
    {
        /// <summary>
        /// ID du jeu pour filtrer
        /// </summary>
        public string? GameId { get; set; }
        
        /// <summary>
        /// ID de la catégorie pour filtrer
        /// </summary>
        public string? CategoryId { get; set; }
        
        /// <summary>
        /// Terme de recherche
        /// </summary>
        public string? SearchTerm { get; set; }
        
        /// <summary>
        /// Critère de tri (recent, popular, name, rating)
        /// </summary>
        public string SortBy { get; set; } = "recent";
        
        /// <summary>
        /// Numéro de page (1-based)
        /// </summary>
        public int PageNumber { get; set; } = 1;
        
        /// <summary>
        /// Nombre d'éléments par page
        /// </summary>
        public int PageSize { get; set; } = 20;
        
        /// <summary>
        /// Tags pour filtrer
        /// </summary>
        public List<string>? Tags { get; set; }
    }
}

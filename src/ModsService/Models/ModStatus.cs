namespace ModsService.Models
{
    /// <summary>
    /// Status d'un mod
    /// </summary>
    public enum ModStatus
    {
        /// <summary>
        /// Brouillon - mod en cours de création
        /// </summary>
        Draft,
        
        /// <summary>
        /// En attente de review/modération
        /// </summary>
        PendingReview,
        
        /// <summary>
        /// Publié et disponible
        /// </summary>
        Published,
        
        /// <summary>
        /// Archivé - plus maintenu mais disponible
        /// </summary>
        Archived,
        
        /// <summary>
        /// Suspendu - temporairement indisponible
        /// </summary>
        Suspended,
        
        /// <summary>
        /// Supprimé - plus disponible
        /// </summary>
        Deleted
    }
}

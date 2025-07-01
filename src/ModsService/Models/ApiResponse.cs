namespace ModsService.Models
{
    /// <summary>
    /// Réponse standard de l'API
    /// </summary>
    /// <typeparam name="T">Type des données de la réponse</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indique si l'opération a réussi
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Message descriptif de la réponse
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Données de la réponse
        /// </summary>
        public T? Data { get; set; }
    }
}

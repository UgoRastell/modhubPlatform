using System.ComponentModel.DataAnnotations;

namespace ModsService.Models
{
    /// <summary>
    /// Note donnée à un mod par un utilisateur
    /// </summary>
    public class Rating
    {
        /// <summary>
        /// Identifiant unique de la note
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur qui a donné la note
        /// </summary>
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du mod noté
        /// </summary>
        [Required]
        public string ModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Note donnée (1-5)
        /// </summary>
        [Range(1, 5, ErrorMessage = "La note doit être comprise entre 1 et 5")]
        public int Value { get; set; }
        
        /// <summary>
        /// Date de création de la note
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

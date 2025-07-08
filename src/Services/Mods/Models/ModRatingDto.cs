using System.ComponentModel.DataAnnotations;

namespace ModsService.Models
{
    /// <summary>
    /// DTO pour la création d'une nouvelle évaluation de mod.
    /// </summary>
    public class ModRatingDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "La note doit être comprise entre 1 et 5.")]
        public int Rating { get; set; }

        /// <summary>
        /// Commentaire optionnel de l'utilisateur. Actuellement non stocké, conservé pour compatibilité frontend.
        /// </summary>
        [StringLength(500)]
        public string? Comment { get; set; }
    }
}

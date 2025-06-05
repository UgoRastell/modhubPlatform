using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class ModRatingRequest
    {
        [Required(ErrorMessage = "La note est requise")]
        [Range(1, 5, ErrorMessage = "La note doit être comprise entre 1 et 5")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Le commentaire ne peut pas dépasser 500 caractères")]
        public string Comment { get; set; } = string.Empty;
    }
}

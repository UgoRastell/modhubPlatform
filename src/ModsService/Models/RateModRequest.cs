using System.ComponentModel.DataAnnotations;

namespace ModsService.Models
{
    /// <summary>
    /// Requête pour noter un mod
    /// </summary>
    public class RateModRequest
    {
        /// <summary>
        /// Note donnée au mod (1-5)
        /// </summary>
        [Required]
        [Range(1, 5, ErrorMessage = "La note doit être comprise entre 1 et 5")]
        public int Rating { get; set; }
    }
}

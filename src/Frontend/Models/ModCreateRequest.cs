using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class ModCreateRequest
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Le slug ne peut pas dépasser 100 caractères")]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description détaillée est requise")]
        [StringLength(5000, ErrorMessage = "La description ne peut pas dépasser 5000 caractères")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description courte est requise")]
        [StringLength(200, ErrorMessage = "La description courte ne peut pas dépasser 200 caractères")]
        public string ShortDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'identifiant du jeu est requis")]
        public string GameId { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'identifiant de la catégorie est requis")]
        public string CategoryId { get; set; } = string.Empty;

        [Url(ErrorMessage = "Format d'URL invalide")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [Url(ErrorMessage = "Format d'URL invalide")]
        public string DocumentationUrl { get; set; } = string.Empty;

        public List<string> ScreenshotUrls { get; set; } = new List<string>();
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        
        public bool IsFeatured { get; set; } = false;
        public bool IsApproved { get; set; } = false;
    }
}

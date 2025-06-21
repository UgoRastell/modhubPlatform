using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class ModCreateRequest
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est requise")]
        [StringLength(5000, ErrorMessage = "La description ne peut pas dépasser 5000 caractères")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description courte est requise")]
        [StringLength(200, ErrorMessage = "La description courte ne peut pas dépasser 200 caractères")]
        public string ShortDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'identifiant du jeu est requis")]
        public string GameId { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> ScreenshotUrls { get; set; } = new List<string>();

        [Required(ErrorMessage = "La version est requise")]
        [StringLength(20, ErrorMessage = "La version ne peut pas dépasser 20 caractères")]
        public string Version { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le lien de téléchargement est requis")]
        [Url(ErrorMessage = "Format d'URL invalide")]
        public string DownloadUrl { get; set; } = string.Empty;

        [Url(ErrorMessage = "Format d'URL invalide")]
        public string DocumentationUrl { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
    }
}

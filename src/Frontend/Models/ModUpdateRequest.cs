using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class ModUpdateRequest
    {
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Le slug ne peut pas dépasser 100 caractères")]
        public string Slug { get; set; } = string.Empty;

        [StringLength(5000, ErrorMessage = "La description ne peut pas dépasser 5000 caractères")]
        public string Description { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "La description courte ne peut pas dépasser 200 caractères")]
        public string ShortDescription { get; set; } = string.Empty;

        public string GameId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> ScreenshotUrls { get; set; } = new List<string>();

        [StringLength(20, ErrorMessage = "La version ne peut pas dépasser 20 caractères")]
        public string Version { get; set; } = string.Empty;

        [Url(ErrorMessage = "Format d'URL invalide")]
        public string DownloadUrl { get; set; } = string.Empty;

        [Url(ErrorMessage = "Format d'URL invalide")]
        public string DocumentationUrl { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        
        public bool IsFeatured { get; set; } = false;
        public bool IsApproved { get; set; } = false;
    }
}

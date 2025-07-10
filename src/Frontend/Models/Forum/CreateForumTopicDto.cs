using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Forum
{
    public class CreateForumTopicDto
    {
        // CategoryId facultatif pour la création rapide d'un topic
        public string? CategoryId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le titre est requis")]
        [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est requis")]
        [StringLength(5000, ErrorMessage = "Le contenu ne peut pas dépasser 5000 caractères")]
        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
        public bool IsPinned { get; set; } = false;
        public bool IsLocked { get; set; } = false;
    }

    public class UpdateForumTopicDto
    {
        [Required(ErrorMessage = "L'ID du topic est requis")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le titre est requis")]
        [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est requis")]
        [StringLength(5000, ErrorMessage = "Le contenu ne peut pas dépasser 5000 caractères")]
        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
        public bool IsPinned { get; set; } = false;
        public bool IsLocked { get; set; } = false;
    }
}

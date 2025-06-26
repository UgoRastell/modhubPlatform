using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Forum
{
    public class CreateForumPostDto
    {
        [Required(ErrorMessage = "L'ID du topic est requis")]
        public string TopicId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est requis")]
        [StringLength(5000, ErrorMessage = "Le contenu ne peut pas dépasser 5000 caractères")]
        public string Content { get; set; } = string.Empty;

        public string? ParentPostId { get; set; } // Pour les réponses à des posts
        public List<string> Attachments { get; set; } = new List<string>();
    }

    public class UpdateForumPostDto
    {
        [Required(ErrorMessage = "L'ID du post est requis")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le contenu est requis")]
        [StringLength(5000, ErrorMessage = "Le contenu ne peut pas dépasser 5000 caractères")]
        public string Content { get; set; } = string.Empty;

        public List<string> Attachments { get; set; } = new List<string>();
    }
}

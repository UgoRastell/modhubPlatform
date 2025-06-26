using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Forum
{
    public class CreateForumCategoryDto
    {
        [Required(ErrorMessage = "Le nom de la catégorie est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est requise")]
        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Le nom de l'icône ne peut pas dépasser 50 caractères")]
        public string IconName { get; set; } = "folder";
    }

    public class UpdateForumCategoryDto
    {
        [Required(ErrorMessage = "L'ID de la catégorie est requis")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom de la catégorie est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "La description est requise")]
        [StringLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string Description { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Le nom de l'icône ne peut pas dépasser 50 caractères")]
        public string IconName { get; set; } = "folder";
    }
}

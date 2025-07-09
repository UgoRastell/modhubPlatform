using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    /// <summary>
    /// DTO utilisé pour la création d'un jeu vidéo depuis l'interface de modération.
    /// </summary>
    public class GameCreateRequest
    {
        /// <summary>
        /// Titre du jeu vidéo.
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "Le titre ne peut pas dépasser 100 caractères.")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description facultative du jeu vidéo.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// URL de la miniature (thumbnail) représentant le jeu.
        /// </summary>
        [Url(ErrorMessage = "Veuillez saisir une URL valide.")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Identifiants des mods associés au jeu.
        /// </summary>
        public List<string> ModIds { get; set; } = new();
    }
}

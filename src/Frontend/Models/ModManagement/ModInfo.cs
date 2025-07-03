using System;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.ModManagement
{
    /// <summary>
    /// Modèle pour la gestion des mods par le créateur
    /// </summary>
    public class ModInfo
    {
        /// <summary>
        /// Identifiant unique du mod
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Titre du mod
        /// </summary>
        [Required(ErrorMessage = "Le titre est requis")]
        [StringLength(100, ErrorMessage = "Le titre ne doit pas dépasser 100 caractères")]
        public string Title { get; set; }

        /// <summary>
        /// Description du mod
        /// </summary>
        [Required(ErrorMessage = "La description est requise")]
        public string Description { get; set; }

        /// <summary>
        /// URL de la miniature/image du mod
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Jeu auquel le mod est destiné
        /// </summary>
        [Required(ErrorMessage = "Le jeu est requis")]
        public string Game { get; set; }

        /// <summary>
        /// Version actuelle du mod
        /// </summary>
        [Required(ErrorMessage = "La version est requise")]
        public string Version { get; set; }

        /// <summary>
        /// Date de création du mod
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière mise à jour du mod
        /// </summary>
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Nombre de téléchargements du mod
        /// </summary>
        public int Downloads { get; set; }

        /// <summary>
        /// Note moyenne des évaluations du mod (sur 5)
        /// </summary>
        public double Rating { get; set; }

        /// <summary>
        /// Nombre d'évaluations
        /// </summary>
        public int RatingCount { get; set; }

        /// <summary>
        /// Prix du mod (0 = gratuit)
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Indique si le mod est en vedette
        /// </summary>
        public bool IsFeatured { get; set; }

        /// <summary>
        /// Statut actuel du mod (Brouillon, En modération, Publié, Rejeté)
        /// </summary>
        public string Status { get; set; } = "Brouillon";

        /// <summary>
        /// Catégorie du mod
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Taille du mod en Mo
        /// </summary>
        public double SizeInMb { get; set; }
    }
}

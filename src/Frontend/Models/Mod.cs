using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    /// <summary>
    /// Modèle représentant un mod dans la plateforme ModHub
    /// </summary>
    public class Mod
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
        /// Date d'acquisition/ajout à la bibliothèque de l'utilisateur
        /// </summary>
        public DateTime AcquiredDate { get; set; } = DateTime.Now;

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
        /// Indique si l'utilisateur a marqué ce mod comme favori
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Statut actuel du mod (Brouillon, En modération, Publié, Rejeté)
        /// </summary>
        public string Status { get; set; } = "Publié";

        /// <summary>
        /// Catégorie du mod
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Tags associés au mod
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Taille du mod en Mo
        /// </summary>
        public double SizeInMb { get; set; }

        /// <summary>
        /// Créateur du mod
        /// </summary>
        public string CreatorName { get; set; }

        /// <summary>
        /// ID du créateur
        /// </summary>
        public string CreatorId { get; set; }
    }
}

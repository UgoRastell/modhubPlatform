using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Ratings
{
    /// <summary>
    /// Avis et évaluation sur un mod
    /// </summary>
    public class ModReview
    {
        /// <summary>
        /// Identifiant unique de l'avis
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du mod concerné
        /// </summary>
        public string ModId { get; set; } = string.Empty;
        
        /// <summary>
        /// Version spécifique du mod concernée (optionnel)
        /// </summary>
        public string? ModVersion { get; set; }
        
        /// <summary>
        /// Note sur 5 étoiles
        /// </summary>
        public int Rating { get; set; } = 0;
        
        /// <summary>
        /// Titre de l'avis
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Contenu de l'avis
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur qui a posté l'avis
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur de l'auteur
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de publication
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Temps d'utilisation estimé (en heures)
        /// </summary>
        public float? UsageTime { get; set; }
        
        /// <summary>
        /// Étiquettes appliquées par l'utilisateur
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Avantages mentionnés
        /// </summary>
        public List<string>? Pros { get; set; }
        
        /// <summary>
        /// Inconvénients mentionnés
        /// </summary>
        public List<string>? Cons { get; set; }
        
        /// <summary>
        /// ID des utilisateurs qui ont trouvé cet avis utile
        /// </summary>
        public List<string> HelpfulVotes { get; set; } = new List<string>();
        
        /// <summary>
        /// ID des utilisateurs qui ont trouvé cet avis inutile
        /// </summary>
        public List<string> UnhelpfulVotes { get; set; } = new List<string>();
        
        /// <summary>
        /// Si l'auteur a acheté/téléchargé officiellement le mod
        /// </summary>
        public bool IsVerifiedPurchase { get; set; } = false;
        
        /// <summary>
        /// Commentaires de réponse sur cet avis
        /// </summary>
        public List<ReviewComment>? Comments { get; set; }
        
        /// <summary>
        /// Si l'avis a été signalé pour modération
        /// </summary>
        public bool IsFlagged { get; set; } = false;
        
        /// <summary>
        /// Raison du signalement
        /// </summary>
        public string? FlagReason { get; set; }
        
        /// <summary>
        /// Si l'avis est approuvé et visible
        /// </summary>
        public bool IsApproved { get; set; } = true;
    }
    
    /// <summary>
    /// Commentaire sur un avis
    /// </summary>
    public class ReviewComment
    {
        /// <summary>
        /// ID unique du commentaire
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Contenu du commentaire
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur auteur du commentaire
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// Si l'utilisateur est le créateur du mod
        /// </summary>
        public bool IsModAuthor { get; set; } = false;
        
        /// <summary>
        /// Date de publication du commentaire
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Si le commentaire a été signalé
        /// </summary>
        public bool IsFlagged { get; set; } = false;
    }
}

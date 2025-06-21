using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Forum
{
    /// <summary>
    /// Message dans un sujet du forum
    /// </summary>
    public class ForumPost
    {
        /// <summary>
        /// Identifiant unique du message
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du sujet auquel appartient ce message
        /// </summary>
        public string TopicId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la catégorie parente
        /// </summary>
        public string CategoryId { get; set; } = string.Empty;
        
        /// <summary>
        /// Contenu du message
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Format du contenu (Markdown, HTML, etc.)
        /// </summary>
        public string ContentFormat { get; set; } = "Markdown";
        
        /// <summary>
        /// ID de l'utilisateur qui a créé ce message
        /// </summary>
        public string CreatedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur de l'auteur
        /// </summary>
        public string CreatedByUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// Si ce message est la solution au sujet (pour les questions)
        /// </summary>
        public bool IsSolution { get; set; } = false;
        
        /// <summary>
        /// ID du message parent (en cas de réponse directe à un message)
        /// </summary>
        public string? ParentPostId { get; set; }
        
        /// <summary>
        /// Liste des utilisateurs mentionnés dans ce message
        /// </summary>
        public List<string> MentionedUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Position dans le fil de discussion (numéro du message)
        /// </summary>
        public int Position { get; set; } = 0;
        
        /// <summary>
        /// Si le message a été signalé pour modération
        /// </summary>
        public bool IsFlagged { get; set; } = false;
        
        /// <summary>
        /// Raison du signalement
        /// </summary>
        public string? FlagReason { get; set; }
        
        /// <summary>
        /// ID des utilisateurs qui ont réagi
        /// </summary>
        public Dictionary<string, List<string>> Reactions { get; set; } = new Dictionary<string, List<string>>();
        
        /// <summary>
        /// Historique des versions du message
        /// </summary>
        public List<PostEditHistory>? EditHistory { get; set; }
        
        /// <summary>
        /// Pièces jointes
        /// </summary>
        public List<PostAttachment>? Attachments { get; set; }
    }
    
    /// <summary>
    /// Historique des modifications d'un message
    /// </summary>
    public class PostEditHistory
    {
        /// <summary>
        /// Contenu précédent
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de la modification
        /// </summary>
        public DateTime EditedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ID de l'utilisateur qui a effectué la modification
        /// </summary>
        public string EditedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Raison de la modification
        /// </summary>
        public string? EditReason { get; set; }
    }
    
    /// <summary>
    /// Pièce jointe à un message
    /// </summary>
    public class PostAttachment
    {
        /// <summary>
        /// ID unique de la pièce jointe
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du fichier
        /// </summary>
        public string Filename { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de la pièce jointe
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Taille du fichier en octets
        /// </summary>
        public long FileSize { get; set; } = 0;
        
        /// <summary>
        /// Type MIME
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        
        /// <summary>
        /// Date d'ajout
        /// </summary>
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

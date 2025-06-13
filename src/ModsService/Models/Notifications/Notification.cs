using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ModsService.Models.Notifications
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        /// <summary>
        /// Type de notification (nouvelle version, mention, etc.)
        /// </summary>
        public NotificationType Type { get; set; }
        
        /// <summary>
        /// Titre de la notification
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Contenu textuel de la notification
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// URL associée à la notification (le cas échéant)
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// ID de l'utilisateur concerné par la notification
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Indique si la notification a été lue
        /// </summary>
        public bool IsRead { get; set; }
        
        /// <summary>
        /// Date de création de la notification
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Date de lecture de la notification (si applicable)
        /// </summary>
        public DateTime? ReadAt { get; set; }
        
        /// <summary>
        /// Identifiants associés (modId, versionId, etc.)
        /// </summary>
        public Dictionary<string, string> RelatedIds { get; set; }
        
        /// <summary>
        /// Données supplémentaires en format JSON
        /// </summary>
        public string AdditionalData { get; set; }
        
        public Notification()
        {
            Id = ObjectId.GenerateNewId().ToString();
            CreatedAt = DateTime.UtcNow;
            IsRead = false;
            RelatedIds = new Dictionary<string, string>();
        }
    }
    
    public enum NotificationType
    {
        NewVersion,
        ModUpdate,
        UserMention,
        ModReview,
        System,
        Other
    }
}

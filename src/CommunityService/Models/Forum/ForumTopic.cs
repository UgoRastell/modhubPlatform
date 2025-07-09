using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Forum
{
    /// <summary>
    /// Sujet de discussion dans le forum
    /// </summary>
    public class ForumTopic
    {
        /// <summary>
        /// Identifiant unique du sujet
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la catégorie à laquelle ce sujet appartient
        /// </summary>
        public string CategoryId { get; set; } = string.Empty;
        
        /// <summary>
        /// Titre du sujet
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Contenu du message initial
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Format du contenu (Markdown, HTML, etc.)
        /// </summary>
        public string ContentFormat { get; set; } = "Markdown";
        
        /// <summary>
        /// ID de l'utilisateur qui a créé ce sujet
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
        /// Si le sujet est épinglé en haut de la catégorie
        /// </summary>
        public bool IsPinned { get; set; } = false;
        
        /// <summary>
        /// Si le sujet est verrouillé (pas de nouvelles réponses)
        /// </summary>
        public bool IsLocked { get; set; } = false;
        
        /// <summary>
        /// Si le sujet est résolu (pour les questions)
        /// </summary>
        public bool IsSolved { get; set; } = false;
        
        /// <summary>
        /// ID du message qui a résolu le sujet (si applicable)
        /// </summary>
        public string? SolutionPostId { get; set; }
        
        /// <summary>
        /// Tags associés au sujet
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// Si le sujet concerne un mod spécifique
        /// </summary>
        public string? RelatedModId { get; set; }
        
        /// <summary>
        /// Si le sujet concerne un jeu spécifique
        /// </summary>
        public string? RelatedGameId { get; set; }
        
        /// <summary>
        /// Si le sujet est officiel (créé par l'équipe)
        /// </summary>
        public bool IsOfficial { get; set; } = false;
        
        /// <summary>
        /// Statistiques du sujet
        /// </summary>
        public TopicStats Stats { get; set; } = new TopicStats();
        
        /// <summary>
        /// ID des utilisateurs qui suivent ce sujet
        /// </summary>
        public List<string> FollowerUserIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Date de la dernière activité (réponse ou modification)
        /// </summary>
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // ----- Legacy properties expected by services (for backward compatibility) -----
        /// <summary>
        /// Nombre total de messages (réponses + message initial).
        /// Cette valeur reflète <c>Stats.ReplyCount + 1</c>.
        /// </summary>
        [BsonIgnore]
        public int PostCount
        {
            get => Stats?.ReplyCount + 1 ?? 1;
            set
            {
                if (Stats == null) Stats = new TopicStats();
                Stats.ReplyCount = Math.Max(0, value - 1);
            }
        }

        /// <summary>
        /// Nombre de vues du sujet (proxy vers <see cref="TopicStats.ViewCount" />).
        /// </summary>
        [BsonIgnore]
        public int ViewCount
        {
            get => Stats?.ViewCount ?? 0;
            set
            {
                if (Stats == null) Stats = new TopicStats();
                Stats.ViewCount = value;
            }
        }

        /// <summary>
        /// Brève description (legacy: certaines requêtes utilisent Description).
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de l'utilisateur de la dernière activité (proxy vers Stats.LastReplyUserId).
        /// </summary>
        [BsonIgnore]
        public string? LastActivityByUserId
        {
            get => Stats?.LastReplyUserId;
            set
            {
                if (Stats == null) Stats = new TopicStats();
                Stats.LastReplyUserId = value;
            }
        }

        /// <summary>
        /// Nom d'utilisateur de la dernière activité (proxy vers Stats.LastReplyUsername).
        /// </summary>
        [BsonIgnore]
        public string? LastActivityByUsername
        {
            get => Stats?.LastReplyUsername;
            set
            {
                if (Stats == null) Stats = new TopicStats();
                Stats.LastReplyUsername = value;
            }
        }
    }
    
    /// <summary>
    /// Statistiques d'un sujet
    /// </summary>
    public class TopicStats
    {
        /// <summary>
        /// Nombre total de réponses
        /// </summary>
        public int ReplyCount { get; set; } = 0;
        
        /// <summary>
        /// Nombre de vues
        /// </summary>
        public int ViewCount { get; set; } = 0;
        
        /// <summary>
        /// ID de l'utilisateur qui a posté la dernière réponse
        /// </summary>
        public string? LastReplyUserId { get; set; }
        
        /// <summary>
        /// Nom d'utilisateur du dernier répondant
        /// </summary>
        public string? LastReplyUsername { get; set; }
        
        /// <summary>
        /// Date de la dernière réponse
        /// </summary>
        public DateTime? LastReplyDate { get; set; }
        
        /// <summary>
        /// ID du dernier message
        /// </summary>
        public string? LastPostId { get; set; }
        
        /// <summary>
        /// Nombre total de mentions "j'aime"
        /// </summary>
        public int LikeCount { get; set; } = 0;
    }
}

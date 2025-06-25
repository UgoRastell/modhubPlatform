using System;
using System.Text.Json.Serialization;

namespace Community.Models.Moderation
{
    public class ContentReport
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Le type de contenu signalé (forum, wiki, commentaire, mod, etc.)
        /// </summary>
        public ContentType ContentType { get; set; }
        
        /// <summary>
        /// L'identifiant du contenu signalé
        /// </summary>
        public string ContentId { get; set; } = string.Empty;
        
        /// <summary>
        /// L'URL du contenu signalé (pour faciliter l'accès)
        /// </summary>
        public string ContentUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Un extrait du contenu signalé (pour faciliter l'identification)
        /// </summary>
        public string ContentSnippet { get; set; } = string.Empty;
        
        /// <summary>
        /// L'identifiant de l'utilisateur qui a fait le signalement
        /// </summary>
        public string ReportedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur de la personne qui a fait le signalement
        /// </summary>
        public string ReportedByUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// L'identifiant de l'utilisateur qui a créé le contenu signalé
        /// </summary>
        public string ContentCreatorUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur de la personne qui a créé le contenu signalé
        /// </summary>
        public string ContentCreatorUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// La raison du signalement
        /// </summary>
        public ReportReason Reason { get; set; }
        
        /// <summary>
        /// Détails supplémentaires fournis par l'utilisateur qui a fait le signalement
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Date et heure du signalement
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statut actuel du signalement
        /// </summary>
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        
        /// <summary>
        /// Date et heure de la dernière mise à jour du statut
        /// </summary>
        public DateTime? StatusUpdatedAt { get; set; }
        
        /// <summary>
        /// L'identifiant du modérateur qui a traité ce signalement
        /// </summary>
        public string? ModeratorUserId { get; set; }
        
        /// <summary>
        /// Nom d'utilisateur du modérateur qui a traité ce signalement
        /// </summary>
        public string? ModeratorUsername { get; set; }
        
        /// <summary>
        /// Notes internes du modérateur
        /// </summary>
        public string? ModeratorNotes { get; set; }
        
        /// <summary>
        /// Action entreprise par le modérateur
        /// </summary>
        public ModeratorAction? Action { get; set; }
        
        /// <summary>
        /// Niveau de priorité du signalement
        /// </summary>
        public ReportPriority Priority { get; set; } = ReportPriority.Medium;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ContentType
    {
        ForumPost,
        WikiPage,
        Comment,
        ModListing,
        UserProfile,
        Message,
        Review,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportReason
    {
        Spam,
        Harassment,
        Violence,
        Pornography,
        IllegalContent,
        ChildAbuse,
        HateSpeech,
        Misinformation,
        Copyright,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportStatus
    {
        Pending,
        InReview,
        Resolved,
        Rejected,
        Duplicate
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ModeratorAction
    {
        NoAction,
        ContentRemoved,
        ContentEdited,
        UserWarned,
        UserSuspended,
        UserBanned,
        ReportRejected,
        EscalatedToAdmin
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReportPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}

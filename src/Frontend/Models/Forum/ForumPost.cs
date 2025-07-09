using System;

namespace Frontend.Models.Forum
{
    /// <summary>
    /// DTO reçu en temps réel via SignalR correspondant au ForumPost côté backend.
    /// Inclut uniquement les champs nécessaires pour l'affichage instantané ;
    /// le rafraîchissement complet REST assurera la cohérence détaillée.
    /// </summary>
    public class ForumPost
    {
        public string Id { get; set; } = string.Empty;
        public string TopicId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

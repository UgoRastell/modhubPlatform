using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Frontend.Models
{
    public class NotificationDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        
        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }
        
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("readAt")]
        public DateTime? ReadAt { get; set; }
        
        [JsonPropertyName("relatedIds")]
        public Dictionary<string, string> RelatedIds { get; set; } = new Dictionary<string, string>();
        
        [JsonPropertyName("additionalData")]
        public string? AdditionalData { get; set; }
        
        // Propriétés calculées pour l'affichage
        public string TimeSince => GetTimeSince(CreatedAt);
        public string IconClass => GetIconClass(Type ?? "System");
        public string TypeClass => GetTypeClass(Type ?? "System");
        
        private string GetTimeSince(DateTime date)
        {
            var timeSpan = DateTime.UtcNow - date;
            
            if (timeSpan.TotalMinutes < 1)
                return "À l'instant";
            if (timeSpan.TotalHours < 1)
                return $"Il y a {(int)timeSpan.TotalMinutes} min";
            if (timeSpan.TotalDays < 1)
                return $"Il y a {(int)timeSpan.TotalHours} h";
            if (timeSpan.TotalDays < 7)
                return $"Il y a {(int)timeSpan.TotalDays} j";
            
            return date.ToString("dd/MM/yyyy");
        }
        
        private string GetIconClass(string type)
        {
            if (type == null) return "fa-bell";
            
            return type switch
            {
                "NewVersion" => "fa-tag",
                "ModUpdate" => "fa-edit",
                "UserMention" => "fa-at",
                "ModReview" => "fa-star",
                "System" => "fa-bell",
                _ => "fa-info-circle"
            };
        }
        
        private string GetTypeClass(string type)
        {
            if (type == null) return "bg-secondary";
            
            return type switch
            {
                "NewVersion" => "bg-success",
                "ModUpdate" => "bg-primary",
                "UserMention" => "bg-info",
                "ModReview" => "bg-warning",
                "System" => "bg-secondary",
                _ => "bg-light"
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Frontend.Models
{
    public class ModDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string CreatorId { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public string CreatorAvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("author")] // Author (fallback si CreatorName non fourni)
        public string Author { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> ScreenshotUrls { get; set; } = new List<string>();
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string DocumentationUrl { get; set; } = string.Empty;
        public long DownloadCount { get; set; }
        
        // Compatible avec Rating du backend
        [JsonPropertyName("rating")]
        public double AverageRating { get; set; }
        
        // Compatible avec ReviewCount du backend
        [JsonPropertyName("reviewCount")]
        public int RatingCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        
        // Compatible avec IsFeatured du frontend et isPremium du backend
        [JsonPropertyName("isPremium")]
        public bool IsFeatured { get; set; }
        
        // Compatible avec isApproved du backend
        [JsonPropertyName("isApproved")]
        public bool IsApproved { get; set; }
        
        // Propriété supplémentaire pour la rétrocompatibilité
        [JsonPropertyName("versions")]
        public List<ModVersionDto> Versions { get; set; } = new List<ModVersionDto>();
        
        // Propriété supplémentaire présente dans la réponse backend
        [JsonPropertyName("isNew")]
        public bool IsNew { get; set; }
    }
}

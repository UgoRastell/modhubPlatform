using System;
using System.Collections.Generic;

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
        public string GameId { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public List<string> ScreenshotUrls { get; set; } = new List<string>();
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string DocumentationUrl { get; set; } = string.Empty;
        public long DownloadCount { get; set; }
        public double AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        public bool IsFeatured { get; set; }
        public bool IsApproved { get; set; }
        public List<ModVersionDto> Versions { get; set; } = new List<ModVersionDto>();
    }
}

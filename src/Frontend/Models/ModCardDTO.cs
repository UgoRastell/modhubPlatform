using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    public class ModCardDTO
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string CoverImageUrl { get; set; }
        public string Status { get; set; } // Published, Draft, Pending, Rejected
        public List<string> Tags { get; set; } = new List<string>();
        public int DownloadCount { get; set; }
        public double Rating { get; set; } // Average rating
        public int RatingCount { get; set; } // Number of ratings
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Helper properties for UI
        public string StatusColor
        {
            get
            {
                return Status.ToLower() switch
                {
                    "published" => "#4CAF50", // Green
                    "draft" => "#FFC107",     // Amber
                    "pending" => "#2196F3",   // Blue
                    "rejected" => "#F44336",  // Red
                    _ => "#9E9E9E"            // Grey
                };
            }
        }
        
        public string FormattedRating => $"{Rating:F1}";
        public string FormattedDownloads => DownloadCount >= 1000 ? $"{DownloadCount / 1000:F1}k" : DownloadCount.ToString();
    }
}

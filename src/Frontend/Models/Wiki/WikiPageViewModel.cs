using System;

namespace Frontend.Models.Wiki
{
    public class WikiPageViewModel
    {
        public required string Id { get; set; }
        public required string CategoryId { get; set; }
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int ViewsCount { get; set; }
        public bool IsFeatured { get; set; }
        public required string[] Tags { get; set; }
        
        // Properties used in WikiIndex.razor
        public required string Slug { get; set; }
        public required string Summary { get; set; }
        public int ViewCount { get; set; } // Alias for ViewsCount for frontend consistency
        public DateTime LastModifiedAt { get; set; } // Alias for LastUpdateDate
        public string? LastModifiedByName { get; set; }
    }
}

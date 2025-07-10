using System;
using System.Collections.Generic;

namespace Frontend.Models.Forum
{
    public class ForumTopicViewModel
    {
        public required string Id { get; set; }
        public string? CategoryId { get; set; } = string.Empty;
        public required string Title { get; set; }
        public string? Content { get; set; }
        public string? AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public int ViewsCount { get; set; }
        public int RepliesCount { get; set; }
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        
        // Properties used in Forums.razor
        public string CategoryName { get; set; } = string.Empty;
        public string PostedDate => CreatedAt.ToString("dd/MM/yyyy");
        
        // Additional properties referenced in ForumIndex.razor
        public string? Slug { get; set; }
        public string? LastReplyAuthorId { get; set; }
        public string? LastReplyAuthorName { get; set; }
        
        // Additional properties referenced in CategoryView.razor
        public bool IsRead { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public int PostCount { get; set; }
        public int ViewCount { get; set; }
        public DateTime LastActivityAt { get; set; }
        public string? LastPostUserId { get; set; }
        public string? LastPostUserName { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Frontend.Models.Forum
{
    public class ForumPostViewModel
    {
        public required string Id { get; set; }
        public required string TopicId { get; set; }
        public required string Content { get; set; }
        public required string AuthorId { get; set; }
        public required string AuthorName { get; set; }
        public string? AuthorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedReason { get; set; }
        public int LikesCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public List<string> Attachments { get; set; } = new List<string>();
        public string? ParentPostId { get; set; } // Pour les réponses à des posts
        public List<ForumPostViewModel> Replies { get; set; } = new List<ForumPostViewModel>();
    }
}

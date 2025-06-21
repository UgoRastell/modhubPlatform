using System;
using System.Collections.Generic;

namespace Frontend.Models.Forum
{
    public class ForumCategoryViewModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string IconName { get; set; } = "folder"; // Default icon name, referenced in CategoryView.razor
        public int TopicsCount { get; set; }
        public int PostsCount { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public required string LastActivityByUserId { get; set; }
        public required string LastActivityByUserName { get; set; }
        public List<ForumTopicViewModel> Topics { get; set; } = new List<ForumTopicViewModel>();
    }
}

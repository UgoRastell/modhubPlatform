namespace Frontend.Models.Forum
{
    public class ForumStatistics
    {
        public int TotalCategories { get; set; }
        public int TotalTopics { get; set; }
        public int TotalPosts { get; set; }
        public int TotalUsers { get; set; }
        public int TotalMembers { get; set; }
        public required string MostActiveCategory { get; set; }
        public required string MostActiveCategoryId { get; set; }
        public required string MostActiveTopic { get; set; }
        public required string MostActiveTopicId { get; set; }
        public required string MostActiveUser { get; set; }
        public required string MostActiveUserId { get; set; }

        // Properties used in ForumIndex.razor and Forums.razor
        public required string NewestMemberId { get; set; }
        public required string NewestMemberName { get; set; }
        public string LastMemberName => NewestMemberName;
        public int OnlineUserCount { get; set; }
        public int OnlineGuestCount { get; set; }
    }
}

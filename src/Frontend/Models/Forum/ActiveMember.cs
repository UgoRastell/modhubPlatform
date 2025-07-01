namespace Frontend.Models.Forum
{
    public class ActiveMember
    {
        public required string Id { get; set; }
        public required string Username { get; set; }
        public required string DisplayName { get; set; }
        public DateTime LastActivityDate { get; set; }
        public int PostCount { get; set; }
        public bool IsOnline { get; set; }
        public string? AvatarUrl { get; set; }
    }
}

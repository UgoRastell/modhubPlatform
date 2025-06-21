namespace ProfileService.Events
{
    public class ProfileUpdatedEvent
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FavoriteAddedEvent
    {
        public string UserId { get; set; }
        public string ModId { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class FavoriteRemovedEvent
    {
        public string UserId { get; set; }
        public string ModId { get; set; }
        public DateTime RemovedAt { get; set; }
    }

    public class UserFollowedEvent
    {
        public string FollowerId { get; set; }
        public string FollowingId { get; set; } // The user being followed
        public DateTime FollowedAt { get; set; }
    }

    public class UserUnfollowedEvent
    {
        public string FollowerId { get; set; }
        public string UnfollowedId { get; set; }
        public DateTime UnfollowedAt { get; set; }
    }

    public class AccountDeletionRequestedEvent
    {
        public string UserId { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class ProfilePrivacyUpdatedEvent
    {
        public string UserId { get; set; }
        public bool IsPublic { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

namespace ProfileService.Repositories
{
    public interface IPreferenceRepository
    {
        Task<UserPreferences> GetPreferencesAsync(string userId);
        Task<UserPreferences> UpdatePreferencesAsync(string userId, UserPreferences preferences);
        Task DeletePreferencesAsync(string userId);
    }
    
    public class UserPreferences
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public NotificationPreferences Notifications { get; set; } = new NotificationPreferences();
        public UiPreferences UiSettings { get; set; } = new UiPreferences();
        public bool UseDefaultSettings { get; set; } = true;
        public List<string> FavoriteTags { get; set; } = new List<string>();
        public List<string> FavoriteGames { get; set; } = new List<string>();
        public List<string> HiddenTags { get; set; } = new List<string>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    public class NotificationPreferences
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool NewFollowerNotifications { get; set; } = true;
        public bool ModUpdateNotifications { get; set; } = true;
        public bool CommentNotifications { get; set; } = true;
        public bool RatingNotifications { get; set; } = true;
        public bool NewsletterSubscription { get; set; } = false;
        public int DailyNotificationLimit { get; set; } = 10;
    }
    
    public class UiPreferences
    {
        public string Theme { get; set; } = "system";
        public string Language { get; set; } = "en";
        public bool UseCompactView { get; set; } = false;
        public int DefaultPageSize { get; set; } = 20;
        public string DefaultSortOrder { get; set; } = "latest";
    }
}

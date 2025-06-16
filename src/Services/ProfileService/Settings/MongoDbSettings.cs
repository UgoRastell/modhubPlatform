namespace ProfileService.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string ProfilesCollection { get; set; } = "profiles";
        public string FavoritesCollection { get; set; } = "favorites";
        public string FollowsCollection { get; set; } = "follows";
        public string HistoryCollection { get; set; } = "history";
        public string PreferencesCollection { get; set; } = "preferences";
    }
}


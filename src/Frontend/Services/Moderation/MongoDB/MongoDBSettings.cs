using System;

namespace Frontend.Services.Moderation.MongoDB
{
    public class MongoDBSettings
    {
        public string ConnectionString { get; set; } = "mongodb://localhost:27017";
        public string DatabaseName { get; set; } = "ModsGamingPlatform";
        public string ModerationCollectionName { get; set; } = "ContentReports";
    }
}

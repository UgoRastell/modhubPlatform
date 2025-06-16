using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace ProfileService.Models
{
    public class Profile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("displayName")]
        public string DisplayName { get; set; }

        [BsonElement("avatarUrl")]
        public string AvatarUrl { get; set; }

        [BsonElement("bio")]
        public string Bio { get; set; }

        [BsonElement("socialLinks")]
        public List<SocialLink> SocialLinks { get; set; } = new List<SocialLink>();

        [BsonElement("experience")]
        public int ExperiencePoints { get; set; }

        [BsonElement("level")]
        public int Level { get; set; }

        [BsonElement("badges")]
        public List<Badge> Badges { get; set; } = new List<Badge>();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("lastActive")]
        public DateTime LastActive { get; set; }

        [BsonElement("stats")]
        public UserStats Stats { get; set; } = new UserStats();

        [BsonElement("isPublic")]
        public bool IsPublic { get; set; } = true;

        [BsonElement("privacySettings")]
        public PrivacySettings PrivacySettings { get; set; } = new PrivacySettings();
    }

    public class SocialLink
    {
        [BsonElement("platform")]
        public string Platform { get; set; }

        [BsonElement("url")]
        public string Url { get; set; }
    }

    public class Badge
    {
        [BsonElement("id")]
        public string Id { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("description")]
        public string Description { get; set; }

        [BsonElement("imageUrl")]
        public string ImageUrl { get; set; }

        [BsonElement("awardedOn")]
        public DateTime AwardedOn { get; set; }
    }

    public class UserStats
    {
        [BsonElement("totalDownloads")]
        public int TotalDownloads { get; set; }

        [BsonElement("totalUploads")]
        public int TotalUploads { get; set; }

        [BsonElement("totalLikes")]
        public int TotalLikes { get; set; }

        [BsonElement("totalComments")]
        public int TotalComments { get; set; }

        [BsonElement("followersCount")]
        public int FollowersCount { get; set; }

        [BsonElement("followingCount")]
        public int FollowingCount { get; set; }
    }

    public class PrivacySettings
    {
        [BsonElement("showActivity")]
        public bool ShowActivity { get; set; } = true;

        [BsonElement("showDownloads")]
        public bool ShowDownloads { get; set; } = true;

        [BsonElement("showFavorites")]
        public bool ShowFavorites { get; set; } = true;

        [BsonElement("showFollowers")]
        public bool ShowFollowers { get; set; } = true;

        [BsonElement("allowDataCollection")]
        public bool AllowDataCollection { get; set; } = true;

        [BsonElement("allowMarketing")]
        public bool AllowMarketing { get; set; } = false;
    }
}

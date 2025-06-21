using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace IdentityService.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("passwordHash")]
        [JsonIgnore]
        public string PasswordHash { get; set; }

        [BsonElement("passwordSalt")]
        [JsonIgnore]
        public string PasswordSalt { get; set; }

        [BsonElement("twoFactorEnabled")]
        public bool TwoFactorEnabled { get; set; }

        [BsonElement("twoFactorKey")]
        [JsonIgnore]
        public string TwoFactorKey { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("lastLogin")]
        public DateTime? LastLogin { get; set; }

        [BsonElement("failedLoginAttempts")]
        public int FailedLoginAttempts { get; set; }

        [BsonElement("lockedUntil")]
        public DateTime? LockedUntil { get; set; }

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;

        [BsonElement("roles")]
        public List<string> Roles { get; set; } = new List<string>();

        [BsonElement("oauthProviders")]
        public List<UserOAuthProvider> OAuthProviders { get; set; } = new List<UserOAuthProvider>();
    }

    public class UserOAuthProvider
    {
        [BsonElement("provider")]
        public string Provider { get; set; }

        [BsonElement("providerId")]
        public string ProviderId { get; set; }

        [BsonElement("email")]
        public string Email { get; set; }

        [BsonElement("connectedAt")]
        public DateTime ConnectedAt { get; set; }

        [BsonElement("lastUsed")]
        public DateTime? LastUsed { get; set; }
    }
}

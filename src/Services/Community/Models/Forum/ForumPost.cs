#if false
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

#if false
namespace CommunityService.Models.Forum
{
    public class ForumPost
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("content")]
        public string Content { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonElement("createdByUsername")]
        public string CreatedByUsername { get; set; } = string.Empty;
    }
}
#endif
#endif

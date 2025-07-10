using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Forum
{
    public class ForumTopic
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("createdByUserId")]
        public string CreatedByUserId { get; set; } = string.Empty;

        [BsonElement("createdByUsername")]
        public string CreatedByUsername { get; set; } = string.Empty;

        [BsonElement("posts")]
        public List<ForumPost> Posts { get; set; } = new();
    }
}

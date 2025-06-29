using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ModsService.Models
{
    public class Mod
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string Name { get; set; } = null!;
        
        public string Description { get; set; } = null!;
        
        public string Author { get; set; } = null!;
        
        public string CreatorId { get; set; } = null!;
        
        public string GameId { get; set; } = null!;
        
        public string GameName { get; set; } = null!;
        
        public string ThumbnailUrl { get; set; } = null!;
        
        public double Rating { get; set; }
        
        public int ReviewCount { get; set; }
        
        public int DownloadCount { get; set; }
        
        public List<string> Tags { get; set; } = new List<string>();
        
        public bool IsPremium { get; set; }
        
        public bool IsNew { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public string Status { get; set; } = "published";
        
        // Métadonnées du fichier
        public string FileLocation { get; set; } = null!;
        
        public string FileName { get; set; } = null!;
        
        public string MimeType { get; set; } = null!;
        
        public long FileSize { get; set; }
        
        public string Version { get; set; } = "1.0";
    }
}

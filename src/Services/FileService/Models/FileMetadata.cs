using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace FileService.Models
{
    public class FileMetadata
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        [Required]
        public string FileName { get; set; }
        
        [Required]
        public string ContentType { get; set; }
        
        [Required]
        public long FileSize { get; set; }
        
        [Required]
        public string BlobName { get; set; }
        
        [Required]
        public string ContainerName { get; set; }
        
        [BsonRepresentation(BsonType.String)]
        public FileType FileType { get; set; }
        
        [Required]
        public string UploadedBy { get; set; }
        
        public string RelatedEntityId { get; set; } // ModId, ProfileId, etc.
        
        public string Url { get; set; }
        
        public string ThumbnailUrl { get; set; }
        
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        
        public string Extension { get; set; }
        
        public string Hash { get; set; } // MD5 or SHA256 hash
        
        public FileStatus Status { get; set; } = FileStatus.Pending;
        
        public bool IsPublic { get; set; }
        
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ProcessedAt { get; set; }
        
        public DateTime? LastAccessedAt { get; set; }
        
        public long DownloadCount { get; set; }
        
        public List<string> Tags { get; set; } = new List<string>();
    }
    
    public enum FileType
    {
        ModFile,
        ModImage,
        ProfileAvatar,
        BannerImage,
        ScreenshotImage,
        Other
    }
    
    public enum FileStatus
    {
        Pending,
        Processing,
        Scanning,
        Available,
        Quarantined, // Failed virus scan
        Invalid, // Failed validation
        Deleted
    }
}

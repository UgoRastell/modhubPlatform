using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FileService.Models
{
    public class ScanResult
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        [BsonRepresentation(BsonType.ObjectId)]
        public string FileMetadataId { get; set; }
        
        public bool IsClean { get; set; }
        
        public ScanStatus Status { get; set; }
        
        public string ScanEngine { get; set; }
        
        public string ScanEngineVersion { get; set; }
        
        public DateTime ScanStartedAt { get; set; }
        
        public DateTime ScanCompletedAt { get; set; }
        
        public int ScanDurationMs { get; set; }
        
        public List<ThreatDetail> DetectedThreats { get; set; } = new List<ThreatDetail>();
        
        public string RawScanResult { get; set; }
        
        public int AttemptCount { get; set; } = 1;
        
        public string ErrorMessage { get; set; }
    }
    
    public class ThreatDetail
    {
        public string ThreatName { get; set; }
        
        public string ThreatType { get; set; }
        
        public ThreatSeverity Severity { get; set; }
        
        public string Path { get; set; } // Location within archive if applicable
    }
    
    public enum ScanStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        TimedOut
    }
    
    public enum ThreatSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}

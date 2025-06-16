using FileService.Models;

namespace FileService.Events
{
    /// <summary>
    /// Base event for all file events
    /// </summary>
    public abstract class FileEventBase
    {
        public string FileId { get; set; }
        public string FileName { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Published when a file is successfully uploaded and initial processing begins
    /// </summary>
    public class FileUploadedEvent : FileEventBase
    {
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string ContainerName { get; set; }
        public string BlobName { get; set; }
        public FileType FileType { get; set; }
        public string RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Published when a file has been scanned for viruses
    /// </summary>
    public class FileScannedEvent : FileEventBase
    {
        public bool IsClean { get; set; }
        public int ThreatCount { get; set; }
        public string ScanId { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public FileType FileType { get; set; }
        public string RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Published when a file is completely processed and ready for use
    /// </summary>
    public class FileProcessedEvent : FileEventBase
    {
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public FileType FileType { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public string RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Published when a file is permanently deleted
    /// </summary>
    public class FileDeletedEvent : FileEventBase
    {
        public string RelatedEntityId { get; set; }
        public FileType FileType { get; set; }
    }

    /// <summary>
    /// Published when file processing fails for any reason
    /// </summary>
    public class FileProcessingFailedEvent : FileEventBase
    {
        public string ErrorMessage { get; set; }
        public string OperationType { get; set; }
        public string ContentType { get; set; }
        public FileType FileType { get; set; }
        public string RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Published when a quarantined file is restored after review
    /// </summary>
    public class FileRestoredEvent : FileEventBase
    {
        public string ReviewerId { get; set; }
        public string RestorationReason { get; set; }
        public string ContentType { get; set; }
        public FileType FileType { get; set; }
        public string RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Published when files are batch processed for an entity
    /// </summary>
    public class EntityFilesBatchProcessedEvent : FileEventBase
    {
        public string EntityId { get; set; }
        public string EntityType { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public List<string> FileIds { get; set; } = new List<string>();
    }
}

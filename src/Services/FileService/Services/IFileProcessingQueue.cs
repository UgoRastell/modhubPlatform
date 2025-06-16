using FileService.Models;

namespace FileService.Services
{
    public interface IFileProcessingQueue
    {
        Task EnqueueFileForProcessingAsync(FileProcessingItem item);
        Task<FileProcessingItem> DequeueAsync(CancellationToken cancellationToken);
        int GetQueueSize();
        Task<IEnumerable<FileProcessingItem>> GetPendingItemsAsync();
    }

    public class FileProcessingItem
    {
        public string FileMetadataId { get; set; }
        public string BlobName { get; set; }
        public string ContainerName { get; set; }
        public FileProcessingOperation Operation { get; set; }
        public int AttemptCount { get; set; }
        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; }
        public Dictionary<string, string> ProcessingOptions { get; set; } = new Dictionary<string, string>();
    }

    public enum FileProcessingOperation
    {
        VirusScan,
        CreateThumbnail,
        ResizeImage,
        ExtractMetadata,
        MoveToPublic,
        ArchiveVerification,
        Delete
    }
}

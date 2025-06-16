using FileService.Models;
using Microsoft.AspNetCore.Http;

namespace FileService.Services
{
    public interface IFileService
    {
        // Upload methods
        Task<FileUploadResult> UploadFileAsync(IFormFile file, FileType fileType, string userId, string relatedEntityId = null);
        Task<FileUploadResult> UploadFileAsync(Stream fileStream, string fileName, string contentType, FileType fileType, string userId, string relatedEntityId = null);
        Task<string> GenerateUploadUrlAsync(string fileName, FileType fileType, string userId, string relatedEntityId = null, TimeSpan? expiry = null);
        
        // Retrieval methods
        Task<FileMetadata> GetFileMetadataAsync(string id);
        Task<IEnumerable<FileMetadata>> GetFilesByEntityIdAsync(string entityId);
        Task<Stream> DownloadFileAsync(string id);
        Task<string> GetDownloadUrlAsync(string id, TimeSpan? expiry = null);
        Task<FileMetadata> GetFileByBlobNameAsync(string blobName);
        Task<IEnumerable<FileMetadata>> SearchFilesAsync(string searchTerm, FileType? fileType = null, int page = 1, int pageSize = 10);
        
        // Image handling methods
        Task<string> GetImageUrlAsync(string id, int? width = null, int? height = null);
        Task<string> GetThumbnailUrlAsync(string id);
        Task<Stream> ResizeImageAsync(string id, int width, int height);
        
        // Status and management methods
        Task<bool> DeleteFileAsync(string id);
        Task<bool> MoveFileToQuarantineAsync(string id, string reason);
        Task<bool> RestoreFileAsync(string id);
        Task<bool> UpdateFileMetadataAsync(string id, Dictionary<string, string> metadata);
        Task<bool> RecordFileAccessAsync(string id); // Updates last accessed time and increments download count
        
        // Batch operations
        Task<IEnumerable<FileMetadata>> BatchGetMetadataAsync(IEnumerable<string> ids);
        Task<bool> BatchDeleteAsync(IEnumerable<string> ids);
        
        // Security and validation
        Task<ScanResult> GetFileScanResultAsync(string id);
        Task<FileValidationStatus> ValidateFilePermissionsAsync(string id, string userId, FileAction action);
    }
    
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string FileId { get; set; }
        public string BlobName { get; set; }
        public string Url { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public FileStatus Status { get; set; }
    }
    
    public enum FileAction
    {
        Download,
        View,
        Delete,
        Update
    }
    
    public class FileValidationStatus
    {
        public bool IsAllowed { get; set; }
        public string Message { get; set; }
    }
}

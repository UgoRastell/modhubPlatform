namespace FileService.Services
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string containerName, string blobName, string contentType);
        Task<Stream> DownloadFileAsync(string containerName, string blobName);
        Task<bool> DeleteFileAsync(string containerName, string blobName);
        Task<bool> FileExistsAsync(string containerName, string blobName);
        Task<string> GenerateDownloadUrlAsync(string containerName, string blobName, TimeSpan expiry);
        Task<string> GenerateUploadUrlAsync(string containerName, string blobName, TimeSpan expiry, long maxSize);
        Task CopyFileAsync(string sourceContainer, string sourceBlobName, string destinationContainer, string destinationBlobName);
        Task<string> GetFileUrlAsync(string containerName, string blobName);
        Task<string> GenerateThumbnailAsync(string containerName, string imageBlobName, int width, int height);
        Task<IDictionary<string, string>> GetMetadataAsync(string containerName, string blobName);
        Task SetMetadataAsync(string containerName, string blobName, IDictionary<string, string> metadata);
        Task<long> GetFileSizeAsync(string containerName, string blobName);
    }
}

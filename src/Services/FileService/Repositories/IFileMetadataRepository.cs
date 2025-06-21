using FileService.Models;

namespace FileService.Repositories
{
    public interface IFileMetadataRepository
    {
        Task<FileMetadata> GetByIdAsync(string id);
        Task<IEnumerable<FileMetadata>> GetByIdsAsync(IEnumerable<string> ids);
        Task<FileMetadata> GetByBlobNameAsync(string blobName);
        Task<IEnumerable<FileMetadata>> GetByEntityIdAsync(string entityId);
        Task<IEnumerable<FileMetadata>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<FileMetadata>> GetByTypeAsync(FileType fileType, int page = 1, int pageSize = 10);
        Task<IEnumerable<FileMetadata>> SearchAsync(string searchTerm, int page = 1, int pageSize = 10);
        Task<IEnumerable<FileMetadata>> GetRecentAsync(int count = 10);
        Task<long> GetTotalSizeByUserIdAsync(string userId);
        Task<FileMetadata> CreateAsync(FileMetadata fileMetadata);
        Task<bool> UpdateAsync(FileMetadata fileMetadata);
        Task<bool> UpdateStatusAsync(string id, FileStatus status);
        Task<bool> IncrementDownloadCountAsync(string id);
        Task<bool> UpdateLastAccessedAsync(string id);
        Task<bool> DeleteAsync(string id);
        Task<bool> DeleteByEntityIdAsync(string entityId);
        Task<bool> PhysicalDeleteAsync(string id); // Permanent deletion
    }
}

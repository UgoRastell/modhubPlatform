using FileService.Models;

namespace FileService.Repositories
{
    public interface IScanResultRepository
    {
        Task<ScanResult> GetByIdAsync(string id);
        Task<ScanResult> GetByFileMetadataIdAsync(string fileMetadataId);
        Task<IEnumerable<ScanResult>> GetByStatusAsync(ScanStatus status, int page = 1, int pageSize = 10);
        Task<IEnumerable<ScanResult>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<ScanResult>> GetFailedScansAsync(DateTime since, int page = 1, int pageSize = 10);
        Task<IEnumerable<ScanResult>> GetDetectedThreatsAsync(DateTime since, int page = 1, int pageSize = 10);
        Task<ScanResult> CreateAsync(ScanResult scanResult);
        Task<bool> UpdateAsync(ScanResult scanResult);
        Task<bool> UpdateStatusAsync(string id, ScanStatus status);
        Task<bool> IncrementAttemptCountAsync(string id);
        Task<bool> DeleteAsync(string id);
        Task<bool> DeleteByFileMetadataIdAsync(string fileMetadataId);
        Task<long> GetThreatCountAsync(DateTime since);
        Task<Dictionary<string, int>> GetThreatStatisticsAsync(DateTime since);
    }
}

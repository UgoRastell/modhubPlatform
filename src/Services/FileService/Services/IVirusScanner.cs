using FileService.Models;

namespace FileService.Services
{
    public interface IVirusScanner
    {
        Task<ScanResult> ScanFileAsync(Stream fileStream, string fileName, string fileMetadataId);
        Task<ScanResult> ScanFileAsync(string containerName, string blobName, string fileMetadataId);
        Task<bool> IsEngineAvailableAsync();
        Task<string> GetEngineVersionAsync();
        Task<Dictionary<string, int>> GetThreatDatabaseStatsAsync();
        Task<DateTime> GetLastDatabaseUpdateAsync();
    }
}

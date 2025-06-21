using Frontend.Models;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public interface IDownloadQuotaService
    {
        Task<ApiResponse<QuotaSettingsDto>> GetQuotaSettingsAsync();
        Task<ApiResponse<DataRetentionSettingsDto>> GetCleanupSettingsAsync();
        Task<ApiResponse<DownloadStatsDto>> GetDownloadStatsAsync();
        Task<ApiResponse<bool>> UpdateQuotaSettingsAsync(QuotaSettingsDto settings);
        Task<ApiResponse<bool>> UpdateCleanupSettingsAsync(DataRetentionSettingsDto settings);
        Task<ApiResponse<int>> RunManualCleanupAsync();
        Task<ApiResponse<PaginatedResult<QuotaEntryDto>>> GetQuotasAsync(string searchTerm, string searchType, int page, int pageSize, string sortField, string sortDirection);
        Task<ApiResponse<bool>> ResetQuotaAsync(string quotaId);
        Task<ApiResponse<bool>> UpdateQuotaAsync(QuotaEntryDto quota);
    }
}

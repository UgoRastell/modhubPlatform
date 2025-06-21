namespace ProfileService.Services
{
    public interface IExportService
    {
        Task<ExportFile> ExportUserDataAsync(string userId);
        Task<ExportFile> ExportProfileDataAsync(string userId);
        Task<ExportFile> ExportDownloadHistoryAsync(string userId);
        Task<ExportFile> ExportFavoritesAsync(string userId);
        Task<ExportFile> ExportFollowsAsync(string userId);
        Task<ExportFile> ExportPreferencesAsync(string userId);
    }
}

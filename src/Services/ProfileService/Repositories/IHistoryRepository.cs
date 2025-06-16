using ProfileService.Models;

namespace ProfileService.Repositories
{
    public interface IHistoryRepository
    {
        Task AddDownloadHistoryAsync(string userId, string modId, string versionId);
        Task<IEnumerable<DownloadHistoryItem>> GetDownloadHistoryAsync(string userId, int page = 1, int pageSize = 20);
        Task AddViewHistoryAsync(string userId, string modId);
        Task<IEnumerable<ModSummary>> GetViewHistoryAsync(string userId, int page = 1, int pageSize = 20);
        Task DeleteHistoryItemAsync(string userId, string historyId);
        Task DeleteAllUserHistoryAsync(string userId);
        Task<IEnumerable<PopularModSummary>> GetPopularDownloadsAsync(int days = 7, int limit = 10);
    }
    
    public class DownloadHistoryItem
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ModId { get; set; }
        public string ModName { get; set; }
        public string ModImageUrl { get; set; }
        public string VersionId { get; set; }
        public string VersionName { get; set; }
        public DateTime DownloadDate { get; set; }
    }
    
    public class PopularModSummary : ModSummary
    {
        public int DownloadCount { get; set; }
    }
    
    public class ModSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public int TotalDownloads { get; set; }
        public double Rating { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

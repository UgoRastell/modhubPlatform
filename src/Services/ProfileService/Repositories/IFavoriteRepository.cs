using ProfileService.Models;

namespace ProfileService.Repositories
{
    public interface IFavoriteRepository
    {
        Task<bool> AddFavoriteAsync(string userId, string modId);
        Task<bool> RemoveFavoriteAsync(string userId, string modId);
        Task<bool> IsFavoriteAsync(string userId, string modId);
        Task<IEnumerable<ModSummary>> GetUserFavoritesAsync(string userId, int page = 1, int pageSize = 20);
        Task<int> CountUserFavoritesAsync(string userId);
        Task<IEnumerable<string>> GetFavoriteModIdsAsync(string userId);
        Task<IEnumerable<string>> GetUserIdsByFavoriteModAsync(string modId, int page = 1, int pageSize = 100);
        Task DeleteAllUserFavoritesAsync(string userId);
    }
}

using ProfileService.Models;

namespace ProfileService.Repositories
{
    public interface IProfileRepository
    {
        Task<Profile> GetByIdAsync(string id);
        Task<Profile> GetByUserIdAsync(string userId);
        Task<Profile> CreateAsync(Profile profile);
        Task<Profile> UpdateAsync(string userId, Profile profile);
        Task<bool> DeleteAsync(string userId);
        Task<IEnumerable<Profile>> GetByIdsAsync(IEnumerable<string> userIds);
        Task UpdateLastActiveAsync(string userId);
        Task<IEnumerable<Profile>> SearchProfilesAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task UpdateStatsAsync(string userId, UserStats stats);
    }
}

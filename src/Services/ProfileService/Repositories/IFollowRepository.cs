using ProfileService.Models;

namespace ProfileService.Repositories
{
    public interface IFollowRepository
    {
        Task<bool> FollowUserAsync(string followerId, string followingId);
        Task<bool> UnfollowUserAsync(string followerId, string followingId);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<IEnumerable<ProfileSummary>> GetFollowersAsync(string userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<ProfileSummary>> GetFollowingAsync(string userId, int page = 1, int pageSize = 20);
        Task<int> CountFollowersAsync(string userId);
        Task<int> CountFollowingAsync(string userId);
        Task DeleteAllFollowsAsync(string userId);
        Task<IEnumerable<string>> GetFollowersIdsAsync(string userId);
        Task<IEnumerable<string>> GetFollowingIdsAsync(string userId);
    }
}

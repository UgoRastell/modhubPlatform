using ProfileService.Services;

namespace ProfileService.Services
{
    public interface IFollowService
    {
        Task FollowUserAsync(string followerId, string followingId);
        Task UnfollowUserAsync(string followerId, string followingId);
        Task<bool> IsFollowingAsync(string followerId, string followingId);
        Task<IEnumerable<ProfileSummary>> GetFollowersAsync(string userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<ProfileSummary>> GetFollowingAsync(string userId, int page = 1, int pageSize = 20);
        Task<int> CountFollowersAsync(string userId);
        Task<int> CountFollowingAsync(string userId);
        Task NotifyNewFollowerAsync(string followerId, string followingId);
        Task<IEnumerable<string>> GetFollowerIdsAsync(string userId);
    }
}

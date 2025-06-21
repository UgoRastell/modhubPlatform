using Frontend.Models;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public interface IUserService
    {
        Task<ApiResponse<UserProfile>> GetUserProfileAsync(string userId);
        Task<ApiResponse<UserProfile>> UpdateUserProfileAsync(UserProfile userProfile);
        Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
        Task<ApiResponse<PagedResult<UserProfile>>> GetUsersAsync(int page, int pageSize, string searchTerm = "");
    }
}

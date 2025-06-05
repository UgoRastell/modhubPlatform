using Frontend.Models;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public interface IModService
    {
        Task<ApiResponse<PagedResult<ModDto>>> GetModsAsync(int page, int pageSize, string searchTerm = "", string category = "", string sortBy = "");
        Task<ApiResponse<ModDto>> GetModAsync(string id);
        Task<ApiResponse<ModDto>> CreateModAsync(ModCreateRequest request);
        Task<ApiResponse<ModDto>> UpdateModAsync(string id, ModUpdateRequest request);
        Task<ApiResponse<bool>> DeleteModAsync(string id);
        Task<ApiResponse<PagedResult<ModDto>>> GetUserModsAsync(string userId, int page, int pageSize);
        Task<ApiResponse<bool>> RateModAsync(string modId, ModRatingRequest request);
    }
}

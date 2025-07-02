using Frontend.Models;
using Frontend.Models.ModManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public interface IModService
    {
        // Méthode pour récupérer les mods d'un créateur
        Task<List<ModInfo>> GetCreatorModsAsync(string creatorId, string status = null);
        
        Task<ApiResponse<PagedResult<ModDto>>> GetModsAsync(int page, int pageSize, string searchTerm = "", string category = "", string sortBy = "");
        Task<ApiResponse<ModDto>> GetModAsync(string id);
        Task<ApiResponse<ModDto>> CreateModAsync(ModCreateRequest request);
        Task<ApiResponse<ModDto>> UpdateModAsync(string id, ModUpdateRequest request);
        Task<ApiResponse<bool>> DeleteModAsync(string id);
        Task<ApiResponse<PagedResult<ModDto>>> GetUserModsAsync(string userId, int page, int pageSize);
        Task<ApiResponse<bool>> RateModAsync(string modId, ModRatingRequest request);
        
        // Méthodes pour les statistiques de téléchargement
        Task<ApiResponse<DownloadStatsDto>> GetModDownloadStatisticsAsync(string modId);
        Task<ApiResponse<DownloadStatsDto>> GetModDownloadStatisticsAsync(string modId, string versionId);
        
        // Méthode alternative pour obtenir un mod par id (peut être utilisée comme alias de GetModAsync)
        Task<ApiResponse<ModDto>> GetModByIdAsync(string id);
        
        // Méthode pour obtenir le changelog d'une version spécifique
        Task<ApiResponse<string>> GetChangelogAsync(string modId, string versionId);
        
        // Méthode pour télécharger un mod
        Task<ApiResponse<string>> DownloadModAsync(string modId, string? versionId = null);
    }
}

using Frontend.Models;
using Frontend.Models.ModManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Interfaces
{
    /// <summary>
    /// Interface définissant les services liés aux mods.
    /// Doit rester parfaitement alignée avec l’implémentation de <see cref="Frontend.Services.ModService"/>.
    /// </summary>
    public interface IModService
    {
        // Marketplace CRUD
        Task<ApiResponse<PagedResult<ModDto>>> GetModsAsync(int page, int pageSize, string searchTerm = "", string category = "", string sortBy = "");
        Task<ApiResponse<ModDto>>              GetModAsync(string id);
        Task<ApiResponse<ModDto>>              CreateModAsync(ModCreateRequest request);
        Task<ApiResponse<ModDto>>              UpdateModAsync(string id, ModDto modDto);
        Task<ApiResponse<bool>>                DeleteModAsync(string id);

        // Bibliothèque & favoris utilisateur
        Task<List<Mod>>                        GetUserFavoritesAsync(string userId);
        Task<ApiResponse<PagedResult<ModDto>>> GetUserModsAsync(string userId, int page = 1, int pageSize = 20);
        Task<ApiResponse<PagedResult<ModDto>>> GetUserModsPagedAsync(string userId, int page, int pageSize);

        // Créateur
        Task<List<ModInfo>>                    GetCreatorModsAsync(string creatorId, string? status = null);

        // Détails & statistiques
        Task<ApiResponse<ModDto>>              GetModByIdAsync(string modId);
        Task<ApiResponse<string>>              GetChangelogAsync(string modId, string versionId);
        Task<ApiResponse<DownloadStatsDto>>    GetModDownloadStatisticsAsync(string modId);
        Task<ApiResponse<DownloadStatsDto>>    GetModDownloadStatisticsAsync(string modId, string versionId);

        // Actions utilisateur
        Task<bool>                             AddToFavoritesAsync(string userId, string modId);
        Task<bool>                             RemoveFromFavoritesAsync(string userId, string modId);
        Task<ApiResponse<bool>>                RateModAsync(string modId, ModRatingRequest request);

        // Téléchargements
        Task<ApiResponse<string>>              DownloadModAsync(string modId, string? versionId = null);
        Task<string>                           DownloadModAsync(string modId);
    }
}

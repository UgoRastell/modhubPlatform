using Frontend.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public class ModService : IModService
    {
        private readonly HttpClient _httpClient;

        public ModService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ApiResponse<PagedResult<ModDto>>> GetModsAsync(int page, int pageSize, string searchTerm = "", string category = "", string sortBy = "")
        {
            try
            {
                var query = $"/api/v1/mods?page={page}&pageSize={pageSize}";
                
                if (!string.IsNullOrWhiteSpace(searchTerm))
                    query += $"&search={Uri.EscapeDataString(searchTerm)}";
                
                if (!string.IsNullOrWhiteSpace(category))
                    query += $"&category={Uri.EscapeDataString(category)}";
                
                if (!string.IsNullOrWhiteSpace(sortBy))
                    query += $"&sortBy={Uri.EscapeDataString(sortBy)}";

                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<ModDto>>>(query);
                return response ?? new ApiResponse<PagedResult<ModDto>> { Success = false, Message = "Échec de la récupération des mods" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<ModDto>> { 
                    Success = false, 
                    Message = $"Erreur: {ex.Message}",
                    Data = new PagedResult<ModDto> { 
                        Items = Array.Empty<ModDto>(),
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                };
            }
        }

        public async Task<ApiResponse<ModDto>> GetModAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<ModDto>>($"api/v1/mods/{id}");
                return response ?? new ApiResponse<ModDto> { Success = false, Message = "Mod non trouvé" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ModDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<ModDto>> CreateModAsync(ModCreateRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/v1/mods", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ModDto>>();
                    return result ?? new ApiResponse<ModDto> { Success = false, Message = "Échec de la création du mod" };
                }
                
                return new ApiResponse<ModDto> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ModDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<ModDto>> UpdateModAsync(string id, ModUpdateRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/v1/mods/{id}", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<ModDto>>();
                    return result ?? new ApiResponse<ModDto> { Success = false, Message = "Échec de la mise à jour du mod" };
                }
                
                return new ApiResponse<ModDto> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<ModDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> DeleteModAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/v1/mods/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return result ?? new ApiResponse<bool> { Success = false, Message = "Échec de la suppression du mod" };
                }
                
                return new ApiResponse<bool> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<PagedResult<ModDto>>> GetUserModsAsync(string userId, int page, int pageSize)
        {
            try
            {
                var query = $"api/users/{userId}/mods?page={page}&pageSize={pageSize}";
                
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<ModDto>>>(query);
                return response ?? new ApiResponse<PagedResult<ModDto>> { Success = false, Message = "Échec de la récupération des mods de l'utilisateur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<ModDto>> { 
                    Success = false, 
                    Message = $"Erreur: {ex.Message}",
                    Data = new PagedResult<ModDto> { 
                        Items = Array.Empty<ModDto>(),
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                };
            }
        }

        public async Task<ApiResponse<bool>> RateModAsync(string modId, ModRatingRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/v1/mods/{modId}/ratings", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return result ?? new ApiResponse<bool> { Success = false, Message = "Échec de l'évaluation du mod" };
                }
                
                return new ApiResponse<bool> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<DownloadStatsDto>> GetModDownloadStatisticsAsync(string modId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<DownloadStatsDto>>($"api/v1/mods/{modId}/statistics");
                return response ?? new ApiResponse<DownloadStatsDto> { Success = false, Message = "Statistiques non trouvées" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DownloadStatsDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }
        
        public async Task<ApiResponse<DownloadStatsDto>> GetModDownloadStatisticsAsync(string modId, string versionId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<DownloadStatsDto>>($"api/v1/mods/{modId}/versions/{versionId}/statistics");
                return response ?? new ApiResponse<DownloadStatsDto> { Success = false, Message = "Statistiques de version non trouvées" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DownloadStatsDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<ModDto>> GetModByIdAsync(string id)
        {
            try 
            {
                // Récupérer les informations de base du mod
                var modResponse = await GetModAsync(id);
                
                if (!modResponse.Success)
                    return modResponse;
                
                // Récupérer les versions du mod
                var versionsResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<ModVersionDto>>>($"api/v1/mods/{id}/versions");
                
                if (modResponse.Data != null) // Vérification de null pour éviter la déréférence
                {
                    if (versionsResponse != null && versionsResponse.Success && versionsResponse.Data != null)
                    {
                        modResponse.Data.Versions = versionsResponse.Data;
                    }
                    else
                    {
                        // Si on ne peut pas récupérer les versions, initialiser une liste vide
                        modResponse.Data.Versions = new List<ModVersionDto>();
                    }
                }
                
                return modResponse;
            }
            catch (Exception ex)
            {
                return new ApiResponse<ModDto> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<string>> GetChangelogAsync(string modId, string versionId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<string>>($"api/v1/mods/{modId}/versions/{versionId}/changelog");
                return response ?? new ApiResponse<string> { Success = false, Message = "Changelog non trouvé" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<string>> DownloadModAsync(string modId, string? versionId = null)
        {
            try
            {
                string url = versionId == null
                    ? $"api/v1/mods/{modId}/download"
                    : $"api/v1/mods/{modId}/versions/{versionId}/download";

                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var downloadUrl = await response.Content.ReadAsStringAsync();
                    return new ApiResponse<string> { Success = true, Data = downloadUrl };
                }
                
                return new ApiResponse<string> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }
    }
}

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
                var query = $"api/mods?page={page}&pageSize={pageSize}";
                
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
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<ModDto>>($"api/mods/{id}");
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
                var response = await _httpClient.PostAsJsonAsync("api/mods", request);
                
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
                var response = await _httpClient.PutAsJsonAsync($"api/mods/{id}", request);
                
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
                var response = await _httpClient.DeleteAsync($"api/mods/{id}");
                
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
                var response = await _httpClient.PostAsJsonAsync($"api/mods/{modId}/ratings", request);
                
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
    }
}

using Frontend.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;

        public UserService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
        }

        public async Task<ApiResponse<UserProfile>> GetUserProfileAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserProfile>>($"api/users/{userId}");
                return response ?? new ApiResponse<UserProfile> { Success = false, Message = "Échec de la récupération du profil" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserProfile> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<UserProfile>> UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/users", userProfile);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfile>>();
                    return result ?? new ApiResponse<UserProfile> { Success = false, Message = "Échec de la mise à jour du profil" };
                }
                
                return new ApiResponse<UserProfile> { Success = false, Message = $"Échec de la mise à jour: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserProfile> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/users/change-password", request);
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                    return result ?? new ApiResponse<bool> { Success = false, Message = "Échec du changement de mot de passe" };
                }
                
                return new ApiResponse<bool> { Success = false, Message = $"Échec: {response.ReasonPhrase}" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<PagedResult<UserProfile>>> GetUsersAsync(int page, int pageSize, string searchTerm = "")
        {
            try
            {
                var query = $"api/users?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query += $"&search={Uri.EscapeDataString(searchTerm)}";
                }
                
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<UserProfile>>>(query);
                return response ?? new ApiResponse<PagedResult<UserProfile>> { Success = false, Message = "Échec de la récupération des utilisateurs" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<UserProfile>> { 
                    Success = false, 
                    Message = $"Erreur: {ex.Message}",
                    Data = new PagedResult<UserProfile> { 
                        Items = Array.Empty<UserProfile>(),
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                };
            }
        }
    }
}

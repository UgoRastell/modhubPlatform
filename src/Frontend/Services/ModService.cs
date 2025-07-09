using Frontend.Models;
using Frontend.Models.ModManagement;
using Frontend.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Frontend.Services
{
    public class ModService : Frontend.Services.Interfaces.IModService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<ModService> _logger;

        public ModService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage, ILogger<ModService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ModsService");
            _localStorage = localStorage;
            _logger = logger;
        }
        
        private async Task SetAuthHeaderAsync()
        {
            try 
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    // Supprimer l'en-tête d'autorisation s'il était défini précédemment et que l'utilisateur n'est pas connecté
                    _httpClient.DefaultRequestHeaders.Remove("Authorization");
                }
            }
            catch (Exception)
            {
                // Si l'accès au localStorage échoue, on continue sans token
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
        }

        public async Task<ApiResponse<PagedResult<ModDto>>> GetModsAsync(int page, int pageSize, string searchTerm = "", string category = "", string sortBy = "")
        {
            try
            {
                // Ajouter l'en-tête d'authentification
                await SetAuthHeaderAsync();
                
                // Debug URL complète
                var query = $"api/v1/mods?page={page}&pageSize={pageSize}";
                
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
                await SetAuthHeaderAsync();
                
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
                await SetAuthHeaderAsync();
                
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

        public async Task<ApiResponse<ModDto>> UpdateModAsync(string id, ModDto modDto)
        {
            try
            {
                await SetAuthHeaderAsync();
                // Envoi du ModDto (structure attendue par le backend)
                string relative = $"api/v1/mods/{id}";
                var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
                if (baseUrl.EndsWith("/mods-service", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - "/mods-service".Length);
                }
                var url = $"{baseUrl}/{relative}";
                var response = await _httpClient.PutAsJsonAsync(url, modDto);
                
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
                await SetAuthHeaderAsync();
                
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

        public async Task<ApiResponse<PagedResult<ModDto>>> GetUserModsPagedAsync(string userId, int page, int pageSize)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                // Correction: use authenticated creator endpoint instead of (nonexistent) users/{id}/mods
var query = $"api/v1/mods/creator?page={page}&pageSize={pageSize}";
                
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
                await SetAuthHeaderAsync();

                var url = $"api/v1/mods/{modId}/ratings";
                var response = await _httpClient.PostAsJsonAsync(url, request);
                
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
                await SetAuthHeaderAsync();
                
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
                await SetAuthHeaderAsync();
                
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
                await SetAuthHeaderAsync();
                
                // Récupérer les informations de base du mod
                var modResponse = await GetModAsync(id);
                
                if (!modResponse.Success)
                    return modResponse;
                
                // Récupérer les versions du mod (peut ne pas être implémenté côté backend)
                try
                {
                    var versionsHttp = await _httpClient.GetAsync($"api/v1/mods/{id}/versions");
                    if (versionsHttp.IsSuccessStatusCode)
                    {
                        var versionsResponse = await versionsHttp.Content.ReadFromJsonAsync<ApiResponse<List<ModVersionDto>>>();
                        if (modResponse.Data != null)
                        {
                            modResponse.Data.Versions = (versionsResponse != null && versionsResponse.Success && versionsResponse.Data != null)
                                ? versionsResponse.Data
                                : new List<ModVersionDto>();
                        }
                    }
                    else if (versionsHttp.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Endpoint non disponible : considérer qu'il n'y a pas (encore) de gestion des versions
                        if (modResponse.Data != null)
                            modResponse.Data.Versions = new List<ModVersionDto>();
                    }
                    else
                    {
                        // Pour d'autres erreurs, on logge mais on continue avec les infos de base
                        _logger.LogWarning("[GetModByIdAsync] Impossible de récupérer les versions du mod {ModId}. Statut: {Status}", id, versionsHttp.StatusCode);
                    }
                }
                catch (Exception exVers)
                {
                    // Ne pas faire échouer toute la requête si la récupération des versions échoue
                    _logger.LogWarning(exVers, "[GetModByIdAsync] Exception lors de la récupération des versions du mod {ModId}", id);
                    if (modResponse.Data != null)
                        modResponse.Data.Versions = new List<ModVersionDto>();
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
                await SetAuthHeaderAsync();
                
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<string>>($"api/v1/mods/{modId}/versions/{versionId}/changelog");
                return response ?? new ApiResponse<string> { Success = false, Message = "Changelog non trouvé" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<string> { Success = false, Message = $"Erreur: {ex.Message}" };
            }
        }

        public Task<ApiResponse<string>> DownloadModAsync(string modId, string? versionId = null)
        {
            try
            {
                // Pour télécharger, nous n'avons pas besoin de faire un appel HTTP préalable ;
                // il suffit de générer l'URL correcte (GET) que le navigateur ouvrira directement.

                string relativeUrl = string.IsNullOrEmpty(versionId)
                    ? $"api/v1/mods/{modId}/download"
                    : $"api/v1/mods/{modId}/versions/{Uri.EscapeDataString(versionId)}/download";

                var baseUrl = _httpClient.BaseAddress?.ToString().TrimEnd('/') ?? string.Empty;
                // Si BaseAddress contient le segment supplémentaire "/mods-service", on le retire pour cibler correctement la route Gateway
                if (baseUrl.EndsWith("/mods-service", StringComparison.OrdinalIgnoreCase))
                {
                    baseUrl = baseUrl.Substring(0, baseUrl.Length - "/mods-service".Length);
                }
                var fullUrl = $"{baseUrl}/{relativeUrl}";

                var result = new ApiResponse<string>
                {
                    Success = true,
                    Data = fullUrl
                };

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                var err = new ApiResponse<string> { Success = false, Message = $"Erreur lors de la génération de l'URL: {ex.Message}" };
                return Task.FromResult(err);
            }
        }
        
        public async Task<List<Mod>> GetUserFavoritesAsync(string userId)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<ModDto>>>($"api/v1/users/{userId}/favorites");
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data.Select(dto => new Mod
                    {
                        Id = dto.Id,
                        Title = dto.Name,
                        Description = dto.Description,
                        ThumbnailUrl = dto.ThumbnailUrl,
                        Game = dto.GameName,
                        Version = dto.Version,
                        CreatedDate = dto.CreatedAt,
                        UpdatedDate = dto.UpdatedAt,
                        Downloads = (int)dto.DownloadCount,
                        Rating = dto.AverageRating,
                        RatingCount = dto.RatingCount,
                        Price = 0, // Prix par défaut, non disponible directement dans ModDto
                        IsFeatured = dto.IsFeatured,
                        Status = dto.IsApproved ? "Published" : "Draft", // Status dérivé de IsApproved
                        Category = dto.Categories?.FirstOrDefault() ?? string.Empty,
                        CreatorName = dto.CreatorName,
                        CreatorId = dto.CreatorId,
                        IsFavorite = true
                    }).ToList();
                }
                
                return new List<Mod>();
            }
            catch (Exception)
            {
                return new List<Mod>();
            }
        }
        
        public async Task<ApiResponse<PagedResult<ModDto>>> GetUserModsAsync(string userId, int page = 1, int pageSize = 20)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                // Appel API avec pagination
                // Correction: backend endpoint for creator's own mods is /api/v1/mods/creator
// The service previously targeted /api/v1/users/{userId}/mods which does not exist and resulted in 404.
// We now target the valid endpoint and adapt the response (ApiResponse<List<ModDto>>) into the expected
// ApiResponse<PagedResult<ModDto>> so that callers remain unaffected.
var rawResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<ModDto>>>($"api/v1/mods/creator?page={page}&pageSize={pageSize}");

// Transform the raw list response into a paged result expected by the UI layer.
ApiResponse<PagedResult<ModDto>> response;
if (rawResponse != null && rawResponse.Success && rawResponse.Data != null)
{
    var items = rawResponse.Data;
    var paged = new PagedResult<ModDto>
    {
        Items = items,
        TotalCount = items.Count,
        PageIndex = page,
        PageSize = pageSize
    };

    response = new ApiResponse<PagedResult<ModDto>>
    {
        Success = true,
        Message = rawResponse.Message ?? "Mods récupérés avec succès",
        Data = paged
    };
}
else
{
    response = new ApiResponse<PagedResult<ModDto>>
    {
        Success = false,
        Message = rawResponse?.Message ?? "Échec de récupération des mods de l'utilisateur",
        Data = new PagedResult<ModDto>
        {
            Items = new List<ModDto>(),
            TotalCount = 0,
            PageIndex = page,
            PageSize = pageSize
        }
    };
}
                
                return response ?? new ApiResponse<PagedResult<ModDto>>
                {
                    Success = false,
                    Message = "Échec de récupération des mods de l'utilisateur",
                    Data = new PagedResult<ModDto>
                    {
                        Items = new List<ModDto>(),
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PagedResult<ModDto>>
                {
                    Success = false,
                    Message = $"Erreur lors de la récupération des mods: {ex.Message}",
                    Data = new PagedResult<ModDto>
                    {
                        Items = new List<ModDto>(),
                        TotalCount = 0,
                        PageIndex = page,
                        PageSize = pageSize
                    }
                };
            }
        }
        
        public async Task<List<ModInfo>> GetCreatorModsAsync(string creatorId, string? status = null)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                string endpoint = $"api/v1/creators/{creatorId}/mods";
                if (!string.IsNullOrEmpty(status))
                {
                    endpoint += $"?status={Uri.EscapeDataString(status)}";
                }
                
                var apiResponse = await _httpClient.GetFromJsonAsync<ApiResponse<List<ModDto>>>(endpoint);
                
                if (apiResponse != null && apiResponse.Success && apiResponse.Data != null)
                {
                    return apiResponse.Data.Select(dto => new ModInfo
                    {
                        Id = dto.Id,
                        Title = dto.Name,
                        Description = dto.Description,
                        ThumbnailUrl = dto.ThumbnailUrl,
                        Game = dto.GameName,
                        Version = dto.Version,
                        CreatedDate = dto.CreatedAt,
                        UpdatedDate = dto.UpdatedAt,
                        Downloads = (int)dto.DownloadCount,
                        Rating = dto.AverageRating,
                        RatingCount = dto.RatingCount,
                        Price = 0, // Default as Price is not in ModDto
                        IsFeatured = dto.IsFeatured,
                        Status = dto.IsApproved ? "Published" : "Draft", // Derive status from IsApproved
                        Category = dto.Categories?.FirstOrDefault() ?? string.Empty
                    }).ToList();
                }
                
                return new List<ModInfo>();
            }
            catch (Exception)
            {
                return new List<ModInfo>();
            }
        }
        
        public async Task<bool> AddToFavoritesAsync(string userId, string modId)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                var response = await _httpClient.PostAsync($"api/v1/users/{userId}/favorites/{modId}", null);
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public async Task<bool> RemoveFromFavoritesAsync(string userId, string modId)
        {
            try
            {
                await SetAuthHeaderAsync();
                
                var response = await _httpClient.DeleteAsync($"api/v1/users/{userId}/favorites/{modId}");
                
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public async Task<string> DownloadModAsync(string modId)
        {
            try
            {
                var result = await DownloadModAsync(modId, null);
                return result.Success ? result.Data : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

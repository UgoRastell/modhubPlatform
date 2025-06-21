using Frontend.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Frontend.Services
{
    public class DownloadQuotaService : IDownloadQuotaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public DownloadQuotaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiBaseUrl = configuration["ApiBaseUrl"] + "/api/downloads/quotas";
        }

        public async Task<ApiResponse<QuotaSettingsDto>> GetQuotaSettingsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<QuotaSettingsDto>>($"{_apiBaseUrl}/settings");
                return response ?? new ApiResponse<QuotaSettingsDto> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<QuotaSettingsDto> { Success = false, Message = $"Erreur lors de la récupération des paramètres de quota: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<DataRetentionSettingsDto>> GetCleanupSettingsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<DataRetentionSettingsDto>>($"{_apiBaseUrl}/cleanup/settings");
                return response ?? new ApiResponse<DataRetentionSettingsDto> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DataRetentionSettingsDto> { Success = false, Message = $"Erreur lors de la récupération des paramètres de nettoyage: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<DownloadStatsDto>> GetDownloadStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<DownloadStatsDto>>($"{_apiBaseUrl}/stats");
                return response ?? new ApiResponse<DownloadStatsDto> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<DownloadStatsDto> { Success = false, Message = $"Erreur lors de la récupération des statistiques de téléchargement: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> UpdateQuotaSettingsAsync(QuotaSettingsDto settings)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/settings", settings);
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur lors de la mise à jour des paramètres de quota: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> UpdateCleanupSettingsAsync(DataRetentionSettingsDto settings)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/cleanup/settings", settings);
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur lors de la mise à jour des paramètres de nettoyage: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<int>> RunManualCleanupAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/cleanup/run", null);
                return await response.Content.ReadFromJsonAsync<ApiResponse<int>>() 
                    ?? new ApiResponse<int> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<int> { Success = false, Message = $"Erreur lors de l'exécution du nettoyage manuel: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<PaginatedResult<QuotaEntryDto>>> GetQuotasAsync(string searchTerm, string searchType, int page, int pageSize, string sortField, string sortDirection)
        {
            try
            {
                string url = $"{_apiBaseUrl}?page={page}&pageSize={pageSize}";
                
                if (!string.IsNullOrEmpty(searchTerm))
                    url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
                
                if (!string.IsNullOrEmpty(searchType))
                    url += $"&searchType={Uri.EscapeDataString(searchType)}";
                
                if (!string.IsNullOrEmpty(sortField))
                    url += $"&sortField={Uri.EscapeDataString(sortField)}";
                
                if (!string.IsNullOrEmpty(sortDirection))
                    url += $"&sortDirection={Uri.EscapeDataString(sortDirection)}";
                
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<PaginatedResult<QuotaEntryDto>>>(url);
                return response ?? new ApiResponse<PaginatedResult<QuotaEntryDto>> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<PaginatedResult<QuotaEntryDto>> { Success = false, Message = $"Erreur lors de la récupération des quotas: {ex.Message}" };
            }
        }

        public async Task<ApiResponse<bool>> ResetQuotaAsync(string quotaId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/{quotaId}/reset", null);
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur lors de la réinitialisation du quota: {ex.Message}" };
            }
        }

        // Implémentation de la méthode UpdateQuotaAsync
        public async Task<ApiResponse<bool>> UpdateQuotaAsync(QuotaEntryDto quota)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"{_apiBaseUrl}/{quota.Id}", quota);
                return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>() 
                    ?? new ApiResponse<bool> { Success = false, Message = "Réponse nulle reçue du serveur" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool> { Success = false, Message = $"Erreur lors de la mise à jour du quota: {ex.Message}" };
            }
        }
    }
}

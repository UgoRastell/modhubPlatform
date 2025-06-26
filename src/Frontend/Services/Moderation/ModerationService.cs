using Frontend.Models.Moderation;
using Frontend.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Frontend.Services.Moderation
{
    public class ModerationService : IModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModerationService> _logger;
        private readonly ILocalStorageService _localStorage;

        public ModerationService(
            HttpClient httpClient,
            ILogger<ModerationService> logger,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        }
        
        private async Task SetAuthHeaderAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    
                    // Pour le débogage temporaire de l'autorisation
                    if (await _localStorage.GetItemAsync<string>("debugModerationRole") == "true")
                    {
                        // Ajouter un en-tête personnalisé pour indiquer au backend que nous sommes en mode débogage
                        if (_httpClient.DefaultRequestHeaders.Contains("X-Debug-Role"))
                        {
                            _httpClient.DefaultRequestHeaders.Remove("X-Debug-Role");
                        }
                        _httpClient.DefaultRequestHeaders.Add("X-Debug-Role", "Moderator");
                        _logger.LogInformation("En-tête de débogage des rôles ajouté pour la modération");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du token d'authentification");
            }
        }
        
        public async Task<ContentReport> ReportContentAsync(CreateReportRequest request)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.PostAsJsonAsync("api/moderation/reports", request);
                response.EnsureSuccessStatusCode();
                
                var report = await response.Content.ReadFromJsonAsync<ContentReport>();
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du signalement");
                throw; // Propagation de l'erreur au lieu d'utiliser MongoDB
            }
        }

        public async Task<ContentReport> GetReportByIdAsync(string reportId)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.GetAsync($"api/moderation/reports/{reportId}");
                
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                    
                response.EnsureSuccessStatusCode();
                
                var report = await response.Content.ReadFromJsonAsync<ContentReport>();
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du signalement {ReportId}", reportId);
                throw; // Propagation de l'erreur au lieu d'utiliser MongoDB
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetMyReportsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.GetAsync($"api/moderation/reports/my?page={page}&pageSize={pageSize}");
                response.EnsureSuccessStatusCode();
                
                var reports = await response.Content.ReadFromJsonAsync<List<ContentReport>>();
                
                // Récupérer les informations de pagination depuis les en-têtes
                var totalCount = 0;
                var totalPages = 0;
                
                if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
                    int.TryParse(totalCountValues.FirstOrDefault(), out totalCount);
                
                if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesValues))
                    int.TryParse(totalPagesValues.FirstOrDefault(), out totalPages);
                
                return (reports ?? new List<ContentReport>(), totalCount, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des signalements de l'utilisateur");
                throw;
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetAllReportsAsync(
            ReportStatus? status = null, 
            ContentType? contentType = null, 
            ReportPriority? priority = null,
            int page = 1, 
            int pageSize = 20)
        {
            try
            {
                await SetAuthHeaderAsync();
                // Construire l'URL avec les paramètres de requête
                var queryString = $"api/moderation/reports?page={page}&pageSize={pageSize}";
                
                if (status.HasValue)
                    queryString += $"&status={status.Value}";
                    
                if (contentType.HasValue)
                    queryString += $"&contentType={contentType.Value}";
                    
                if (priority.HasValue)
                    queryString += $"&priority={priority.Value}";
                
                var response = await _httpClient.GetAsync(queryString);
                response.EnsureSuccessStatusCode();
                
                var reports = await response.Content.ReadFromJsonAsync<List<ContentReport>>();
                
                // Récupérer les informations de pagination depuis les en-têtes
                var totalCount = 0;
                var totalPages = 0;
                
                if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
                    int.TryParse(totalCountValues.FirstOrDefault(), out totalCount);
                
                if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesValues))
                    int.TryParse(totalPagesValues.FirstOrDefault(), out totalPages);
                
                return (reports ?? new List<ContentReport>(), totalCount, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des signalements");
                
                // En cas d'erreur, retourner une liste vide
                return (new List<ContentReport>(), 0, 0);
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetReportsForModerationAsync(
            ReportStatus? status = null, 
            string contentType = null, 
            int page = 1, 
            int pageSize = 20)
        {
            try
            {
                // Construire l'URL avec les paramètres de filtrage
                var queryParams = new List<string>();
                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");
                
                if (status.HasValue)
                    queryParams.Add($"status={status.Value}");
                
                if (!string.IsNullOrEmpty(contentType))
                    queryParams.Add($"contentType={contentType}");
                
                var url = $"api/moderation/reports/moderation?{string.Join("&", queryParams)}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var reports = await response.Content.ReadFromJsonAsync<List<ContentReport>>();
                
                // Récupérer les informations de pagination depuis les en-têtes
                var totalCount = 0;
                var totalPages = 0;
                
                if (response.Headers.TryGetValues("X-Total-Count", out var totalCountValues))
                    int.TryParse(totalCountValues.FirstOrDefault(), out totalCount);
                
                if (response.Headers.TryGetValues("X-Total-Pages", out var totalPagesValues))
                    int.TryParse(totalPagesValues.FirstOrDefault(), out totalPages);
                
                return (reports ?? new List<ContentReport>(), totalCount, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des signalements pour modération");
                throw;
            }
        }

        public async Task UpdateReportStatusAsync(string reportId, UpdateReportStatusRequest request)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/status", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du signalement {ReportId}", reportId);
                throw;
            }
        }

        public async Task TakeModeratorActionAsync(string reportId, ModeratorActionRequest request)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/action", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prise d'action sur le signalement {ReportId}", reportId);
                throw;
            }
        }

        public async Task UpdateReportPriorityAsync(string reportId, UpdateReportPriorityRequest request)
        {
            try
            {
                await SetAuthHeaderAsync();
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/priority", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la priorité du signalement {ReportId}", reportId);
                throw;
            }
        }

        public async Task<ModerationStatistics> GetModerationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                await SetAuthHeaderAsync();
                // Construire l'URL avec les paramètres de requête
                var queryString = "api/moderation/statistics";
                var hasParam = false;
                
                if (startDate.HasValue)
                {
                    queryString += $"?startDate={startDate.Value:yyyy-MM-ddTHH:mm:ss}";
                    hasParam = true;
                }
                
                if (endDate.HasValue)
                {
                    queryString += hasParam ? $"&endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss}" : $"?endDate={endDate.Value:yyyy-MM-ddTHH:mm:ss}";
                }
                
                var statistics = await _httpClient.GetFromJsonAsync<ModerationStatistics>(queryString);
                return statistics ?? new ModerationStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de modération");
                throw;
            }
        }
    }
}
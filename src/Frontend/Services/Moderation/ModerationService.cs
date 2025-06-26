using Frontend.Models.Moderation;
using Frontend.Services.Moderation.MongoDB;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Frontend.Services.Moderation
{
    public class ModerationService : IModerationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModerationService> _logger;
        private readonly bool _useMockData;
        private readonly ModerationMongoDBService _mongoDBService;

        public ModerationService(
            IHttpClientFactory httpClientFactory, 
            ILogger<ModerationService> logger,
            ModerationMongoDBService mongoDBService)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = _httpClientFactory.CreateClient("CommunityService");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mongoDBService = mongoDBService ?? throw new ArgumentNullException(nameof(mongoDBService));
            _useMockData = true; // Activer les données fictives tant que l'API n'est pas disponible
            
            // Initialiser les données mock si la collection est vide
            _mongoDBService.InitializeMockDataAsync().GetAwaiter().GetResult();
        }
        
        public async Task<ContentReport> ReportContentAsync(CreateReportRequest request)
        {
            try
            {
                if (_useMockData)
                {
                    return await _mongoDBService.CreateReportAsync(request);
                }

                var response = await _httpClient.PostAsJsonAsync("api/moderation/reports", request);
                response.EnsureSuccessStatusCode();
                
                var report = await response.Content.ReadFromJsonAsync<ContentReport>();
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du signalement");
                
                // En cas d'erreur, utiliser MongoDB
                return await _mongoDBService.CreateReportAsync(request);
            }
        }

        public async Task<ContentReport> GetReportByIdAsync(string reportId)
        {
            try
            {
                if (_useMockData)
                {
                    return await _mongoDBService.GetReportByIdAsync(reportId);
                }

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
                
                // En cas d'erreur, utiliser MongoDB
                return await _mongoDBService.GetReportByIdAsync(reportId);
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetMyReportsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                if (_useMockData)
                {
                    // Utiliser un ID utilisateur fictif pour les tests
                    const string mockUserId = "user-1";
                    return await _mongoDBService.GetUserReportsAsync(mockUserId, page, pageSize);
                }
                
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
                
                // En cas d'erreur, utiliser MongoDB avec un utilisateur fictif
                const string mockUserId = "user-1";
                return await _mongoDBService.GetUserReportsAsync(mockUserId, page, pageSize);
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
                if (_useMockData)
                {
                    return await _mongoDBService.GetReportsAsync(status, contentType, priority, page, pageSize);
                }
                
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
                
                // En cas d'erreur, utiliser MongoDB
                return await _mongoDBService.GetReportsAsync(status, contentType, priority, page, pageSize);
            }
        }

        public async Task UpdateReportStatusAsync(string reportId, UpdateReportStatusRequest request)
        {
            try
            {
                if (_useMockData)
                {
                    await _mongoDBService.UpdateReportStatusAsync(reportId, request);
                    return;
                }
                
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/status", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du signalement {ReportId}", reportId);
                
                // En cas d'erreur, essayer d'utiliser MongoDB
                await _mongoDBService.UpdateReportStatusAsync(reportId, request);
            }
        }

        public async Task TakeModeratorActionAsync(string reportId, ModeratorActionRequest request)
        {
            try
            {
                if (_useMockData)
                {
                    await _mongoDBService.TakeModeratorActionAsync(reportId, request);
                    return;
                }
                
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/action", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prise d'action sur le signalement {ReportId}", reportId);
                
                // En cas d'erreur, essayer d'utiliser MongoDB
                await _mongoDBService.TakeModeratorActionAsync(reportId, request);
            }
        }

        public async Task UpdateReportPriorityAsync(string reportId, UpdateReportPriorityRequest request)
        {
            try
            {
                if (_useMockData)
                {
                    await _mongoDBService.UpdateReportPriorityAsync(reportId, request);
                    return;
                }
                
                var response = await _httpClient.PutAsJsonAsync($"api/moderation/reports/{reportId}/priority", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la priorité du signalement {ReportId}", reportId);
                
                // En cas d'erreur, essayer d'utiliser MongoDB
                await _mongoDBService.UpdateReportPriorityAsync(reportId, request);
            }
        }

        public async Task<ModerationStatistics> GetModerationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (_useMockData)
                {
                    return await _mongoDBService.GetModerationStatisticsAsync(startDate, endDate);
                }
                
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
                
                // En cas d'erreur, utiliser MongoDB
                return await _mongoDBService.GetModerationStatisticsAsync(startDate, endDate);
            }
        }
    }
}
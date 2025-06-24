using Frontend.Models.Moderation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Frontend.Services.Moderation
{
    public class ModerationService : IModerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ModerationService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ModerationService(IHttpClientFactory httpClientFactory, ILogger<ModerationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = _httpClientFactory.CreateClient("CommunityService");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ContentReport> ReportContentAsync(CreateReportRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/moderation/reports", request);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<ContentReport>();
                return result ?? new ContentReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du signalement de contenu");
                throw;
            }
        }

        public async Task<ContentReport> GetReportByIdAsync(string reportId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<ContentReport>($"api/moderation/reports/{reportId}");
                return result ?? new ContentReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du signalement {ReportId}", reportId);
                throw;
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetMyReportsAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/moderation/reports/me?page={page}&pageSize={pageSize}");
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
                _logger.LogError(ex, "Erreur lors de la récupération de mes signalements");
                throw;
            }
        }

        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetAllReportsAsync(
            ReportStatus? status = null, ContentType? contentType = null, ReportPriority? priority = null,
            int page = 1, int pageSize = 20)
        {
            try
            {
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
                throw;
            }
        }

        public async Task UpdateReportStatusAsync(string reportId, UpdateReportStatusRequest request)
        {
            try
            {
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

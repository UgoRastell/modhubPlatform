using Frontend.Models.Moderation;
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
        private readonly Random _random = new Random();

        public ModerationService(IHttpClientFactory httpClientFactory, ILogger<ModerationService> logger)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = _httpClientFactory.CreateClient("CommunityService");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _useMockData = true; // Activer les données fictives tant que l'API n'est pas disponible
        }
        
        /// <summary>
        /// Génère des données de test pour les signalements
        /// </summary>
        private (List<ContentReport> Reports, int TotalCount, int TotalPages) GetMockReports(
            ReportStatus? status = null, ContentType? contentType = null, ReportPriority? priority = null,
            int page = 1, int pageSize = 20)
        {
            // Générer une liste de 100 signalements fictifs
            var allReports = new List<ContentReport>();
            
            for (int i = 1; i <= 100; i++)
            {
                var reportStatus = (ReportStatus)(_random.Next(5));
                var reportContentType = (ContentType)(_random.Next(8));
                var reportPriority = (ReportPriority)(_random.Next(4));
                
                // Si des filtres sont appliqués, sauter les éléments qui ne correspondent pas
                if (status.HasValue && reportStatus != status.Value) continue;
                if (contentType.HasValue && reportContentType != contentType.Value) continue;
                if (priority.HasValue && reportPriority != priority.Value) continue;
                
                var report = new ContentReport
                {
                    Id = $"mock-{i}",
                    ContentType = reportContentType,
                    ContentId = $"content-{i}",
                    ContentUrl = $"/content/{reportContentType}/{i}",
                    ContentSnippet = $"Extrait du contenu signalé {i}",
                    ReportedByUserId = $"user-{_random.Next(1, 20)}",
                    ReportedByUsername = $"Utilisateur{_random.Next(1, 20)}",
                    ContentCreatorUserId = $"creator-{_random.Next(1, 10)}",
                    ContentCreatorUsername = $"Créateur{_random.Next(1, 10)}",
                    Reason = (ReportReason)(_random.Next(10)),
                    Description = $"Description du signalement {i}",
                    CreatedAt = DateTime.Now.AddDays(-_random.Next(30)),
                    Status = reportStatus,
                    StatusUpdatedAt = reportStatus != ReportStatus.Pending ? DateTime.Now.AddDays(-_random.Next(10)) : null,
                    ModeratorUserId = reportStatus != ReportStatus.Pending ? $"mod-{_random.Next(1, 5)}" : null,
                    ModeratorUsername = reportStatus != ReportStatus.Pending ? $"Modérateur{_random.Next(1, 5)}" : null,
                    ModeratorNotes = reportStatus != ReportStatus.Pending ? $"Notes du modérateur {i}" : null,
                    Priority = reportPriority
                };
                
                allReports.Add(report);
            }
            
            // Appliquer la pagination
            var totalCount = allReports.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            
            var paginatedReports = allReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            return (paginatedReports, totalCount, totalPages);
        }
        
        /// <summary>
        /// Génère un rapport de contenu fictif avec l'ID spécifié
        /// </summary>
        private ContentReport GenerateMockReport(string reportId)
        {
            // Extraire le numéro de l'ID si possible, sinon utiliser un nombre aléatoire
            int reportNumber;
            if (reportId.StartsWith("mock-") && int.TryParse(reportId.Substring(5), out reportNumber))
            {
                // Utiliser le numéro extrait
            }
            else
            {
                // Génération d'un numéro aléatoire si l'ID ne suit pas le format attendu
                reportNumber = _random.Next(1, 100);
            }

            var reportStatus = (ReportStatus)(_random.Next(5));
            var reportContentType = (ContentType)(_random.Next(8));
            
            return new ContentReport
            {
                Id = reportId,
                ContentType = reportContentType,
                ContentId = $"content-{reportNumber}",
                ContentUrl = $"/content/{reportContentType}/{reportNumber}",
                ContentSnippet = $"Extrait du contenu signalé {reportNumber}",
                ReportedByUserId = $"user-{_random.Next(1, 20)}",
                ReportedByUsername = $"Utilisateur{_random.Next(1, 20)}",
                ContentCreatorUserId = $"creator-{_random.Next(1, 10)}",
                ContentCreatorUsername = $"Créateur{_random.Next(1, 10)}",
                Reason = (ReportReason)(_random.Next(10)),
                Description = $"Description du signalement {reportNumber} (détails)",
                CreatedAt = DateTime.Now.AddDays(-_random.Next(30)),
                Status = reportStatus,
                StatusUpdatedAt = reportStatus != ReportStatus.Pending ? DateTime.Now.AddDays(-_random.Next(10)) : null,
                ModeratorUserId = reportStatus != ReportStatus.Pending ? $"mod-{_random.Next(1, 5)}" : null,
                ModeratorUsername = reportStatus != ReportStatus.Pending ? $"Modérateur{_random.Next(1, 5)}" : null,
                ModeratorNotes = reportStatus != ReportStatus.Pending ? $"Notes du modérateur pour le rapport {reportNumber}" : null,
                Priority = (ReportPriority)(_random.Next(4))
            };
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
                // Si le mode mock est actif, retourner un signalement fictif
                if (_useMockData)
                {
                    _logger.LogWarning("API de modération non disponible - utilisation de données fictives");
                    return GenerateMockReport(reportId);
                }
                
                var response = await _httpClient.GetAsync($"api/moderation/reports/{reportId}");
                
                // Si l'API retourne 404, utiliser des données fictives
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("API de modération non disponible (404) - utilisation de données fictives");
                    return GenerateMockReport(reportId);
                }
                
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<ContentReport>();
                return result ?? new ContentReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du signalement {ReportId}", reportId);
                return GenerateMockReport(reportId);
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
                // Si le mode mock est actif, retourner des données fictives
                if (_useMockData)
                {
                    _logger.LogWarning("API de modération non disponible - utilisation de données fictives");
                    return GetMockReports(status, contentType, priority, page, pageSize);
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

                // Si l'API retourne 404, utiliser les données fictives
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("API de modération non disponible (404) - utilisation de données fictives");
                    return GetMockReports(status, contentType, priority, page, pageSize);
                }
                
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
                // En cas d'erreur, utiliser des données fictives plutôt que de planter
                _logger.LogWarning("Utilisation de données fictives suite à une erreur");
                return GetMockReports(status, contentType, priority, page, pageSize);
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

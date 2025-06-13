using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Repositories;
using Newtonsoft.Json;

namespace ModsService.Services.Download
{
    /// <summary>
    /// Service de gestion des téléchargements avec quotas et statistiques
    /// </summary>
    public class DownloadService : IDownloadService
    {
        private readonly IModRepository _modRepository;
        private readonly IDownloadQuotaRepository _quotaRepository;
        private readonly IDownloadHistoryRepository _historyRepository;
        private readonly ILogger<DownloadService> _logger;
        
        public DownloadService(
            IModRepository modRepository,
            IDownloadQuotaRepository quotaRepository,
            IDownloadHistoryRepository historyRepository,
            ILogger<DownloadService> logger)
        {
            _modRepository = modRepository;
            _quotaRepository = quotaRepository;
            _historyRepository = historyRepository;
            _logger = logger;
        }
        
        /// <inheritdoc />
        public async Task<DownloadResult> RecordDownloadAsync(
            string modId, 
            string versionNumber, 
            string userId,
            HttpContext httpContext)
        {
            try
            {
                var result = new DownloadResult
                {
                    IsAllowed = true
                };
                
                // Vérifier si l'utilisateur a récemment téléchargé ce mod
                if (!string.IsNullOrEmpty(userId))
                {
                    var recentlyDownloaded = await _historyRepository.HasUserRecentlyDownloadedModAsync(
                        userId,
                        modId,
                        null, // Toutes les versions
                        TimeSpan.FromHours(1) // Dans la dernière heure
                    );
                    
                    // Si téléchargement récent, ne pas comptabiliser dans les quotas
                    if (!recentlyDownloaded)
                    {
                        // Vérifier et incrémenter les quotas
                        var dailyQuotaResult = await CheckUserQuotaAsync(userId, QuotaType.Daily);
                        
                        if (!dailyQuotaResult.IsAllowed)
                        {
                            return new DownloadResult
                            {
                                IsAllowed = false,
                                QuotaExceeded = true,
                                RemainingQuota = dailyQuotaResult.RemainingQuota,
                                Message = $"Quota journalier dépassé. Prochaine réinitialisation: {dailyQuotaResult.NextReset:g}"
                            };
                        }
                        
                        // Incrémenter le quota utilisé
                        await _quotaRepository.IncrementQuotaUsageAsync(userId, QuotaType.Daily);
                        await _quotaRepository.IncrementQuotaUsageAsync(userId, QuotaType.Weekly);
                        await _quotaRepository.IncrementQuotaUsageAsync(userId, QuotaType.Monthly);
                        
                        // Mettre à jour le quota restant dans le résultat
                        result.RemainingQuota = dailyQuotaResult.RemainingQuota - 1;
                    }
                    else
                    {
                        // Informer que ce téléchargement était récent et n'affecte pas les quotas
                        result.Message = "Téléchargement récent, les quotas ne sont pas affectés";
                        
                        // Récupérer le quota restant
                        var quotaInfo = await _quotaRepository.GetQuotaByTypeAsync(userId, QuotaType.Daily);
                        result.RemainingQuota = quotaInfo?.RemainingQuota ?? 0;
                    }
                }
                
                // Récupérer le mod et la version
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    return new DownloadResult
                    {
                        IsAllowed = false,
                        Message = "Mod introuvable"
                    };
                }
                
                // Trouver la version spécifique
                var version = mod.Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
                
                if (version == null)
                {
                    return new DownloadResult
                    {
                        IsAllowed = false,
                        Message = $"Version {versionNumber} introuvable pour ce mod"
                    };
                }
                
                // Créer un enregistrement d'historique
                var downloadRecord = new DownloadHistory
                {
                    ModId = modId,
                    ModName = mod.Name,
                    VersionNumber = versionNumber,
                    VersionId = version.Id,
                    UserId = userId,
                    AnonymizedIp = AnonymizeIp(httpContext.Connection.RemoteIpAddress?.ToString()),
                    DownloadedAt = DateTime.UtcNow,
                    DeviceType = GetDeviceType(httpContext),
                    UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
                    Country = GetCountryFromHeaders(httpContext),
                    Referrer = httpContext.Request.Headers["Referer"].ToString(),
                    FileSizeBytes = version.TotalSizeBytes,
                    Status = DownloadStatus.Started,
                    IsResume = httpContext.Request.Headers.ContainsKey("Range")
                };
                
                // Enregistrer le téléchargement
                var savedRecord = await _historyRepository.AddDownloadRecordAsync(downloadRecord);
                result.DownloadId = savedRecord.Id;
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error recording download for mod {modId}, version {versionNumber}");
                
                return new DownloadResult
                {
                    IsAllowed = false,
                    Message = "Erreur lors de l'enregistrement du téléchargement"
                };
            }
        }
        
        /// <inheritdoc />
        public async Task<QuotaCheckResult> CheckUserQuotaAsync(string userId, QuotaType quotaType)
        {
            try
            {
                // Récupérer le quota
                var quota = await _quotaRepository.GetQuotaByTypeAsync(userId, quotaType);
                
                // Si le quota n'existe pas, créer un quota par défaut
                if (quota == null)
                {
                    // Créer un quota par défaut avec les limites standards
                    int limit = quotaType switch
                    {
                        QuotaType.Daily => 20,
                        QuotaType.Weekly => 100,
                        QuotaType.Monthly => 500,
                        _ => 20
                    };
                    
                    // Créer un nouveau quota avec utilisation à 0
                    quota = new DownloadQuota
                    {
                        UserId = userId,
                        Type = quotaType,
                        Limit = limit,
                        CurrentUsage = 0,
                        LastReset = DateTime.UtcNow,
                        NextReset = CalculateNextReset(quotaType),
                        IsBlocking = true
                    };
                    
                    await _quotaRepository.UpsertQuotaAsync(quota);
                    
                    return new QuotaCheckResult
                    {
                        IsAllowed = true,
                        CurrentUsage = 0,
                        Limit = limit,
                        NextReset = quota.NextReset,
                        QuotaType = quotaType
                    };
                }
                
                // Vérifier si le quota doit être réinitialisé
                if (DateTime.UtcNow >= quota.NextReset)
                {
                    // Réinitialiser le quota
                    quota.LastReset = DateTime.UtcNow;
                    quota.NextReset = CalculateNextReset(quotaType);
                    quota.CurrentUsage = 0;
                    
                    await _quotaRepository.UpsertQuotaAsync(quota);
                }
                
                // Vérifier si le quota est dépassé et bloquant
                bool isAllowed = !(quota.HasExceededQuota && quota.IsBlocking);
                
                return new QuotaCheckResult
                {
                    IsAllowed = isAllowed,
                    CurrentUsage = quota.CurrentUsage,
                    Limit = quota.Limit,
                    NextReset = quota.NextReset,
                    QuotaType = quotaType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking {quotaType} quota for user {userId}");
                
                // En cas d'erreur, autoriser le téléchargement avec un message d'avertissement
                return new QuotaCheckResult
                {
                    IsAllowed = true,
                    CurrentUsage = 0,
                    Limit = 20, // Valeur par défaut
                    NextReset = DateTime.UtcNow.AddDays(1),
                    QuotaType = quotaType
                };
            }
        }
        
        /// <inheritdoc />
        public async Task<DownloadStatistics> GetModDownloadStatisticsAsync(
            string modId,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // Pour l'instant, retourne un objet vide, à implémenter réellement
            // en utilisant les méthodes du repository
            return new DownloadStatistics
            {
                ModId = modId,
                LastUpdated = DateTime.UtcNow
            };
        }
        
        /// <inheritdoc />
        public async Task<PaginatedResult<DownloadHistory>> GetUserDownloadHistoryAsync(
            string userId,
            int page = 1,
            int pageSize = 20)
        {
            try
            {
                // Calculer le nombre d'éléments à sauter
                int skip = (page - 1) * pageSize;
                
                // Récupérer les téléchargements de l'utilisateur
                var downloads = await _historyRepository.GetUserDownloadHistoryAsync(
                    userId,
                    null,
                    null,
                    skip,
                    pageSize);
                    
                // Compter le nombre total de téléchargements
                var totalCount = 0;
                
                // Cette implémentation est temporaire et inefficace pour de gros volumes
                // Il faudrait ajouter une méthode CountUserDownloadsAsync au repository
                var filterBuilder = Builders<DownloadHistory>.Filter;
                var filter = filterBuilder.Eq(h => h.UserId, userId);
                
                return new PaginatedResult<DownloadHistory>
                {
                    Items = downloads,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting download history for user {userId}");
                
                return new PaginatedResult<DownloadHistory>
                {
                    Items = new List<DownloadHistory>(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 0
                };
            }
        }
        
        /// <inheritdoc />
        public async Task<byte[]> GenerateDownloadReportAsync(
            string modId,
            DateTime startDate,
            DateTime endDate,
            string format = "csv")
        {
            try
            {
                // Cette méthode nécessiterait l'implémentation des statistiques complètes
                // Pour l'instant, on génère un rapport minimal au format demandé
                
                if (format.ToLower() == "json")
                {
                    var stats = await GetModDownloadStatisticsAsync(modId, startDate, endDate);
                    var json = JsonConvert.SerializeObject(stats, Formatting.Indented);
                    return Encoding.UTF8.GetBytes(json);
                }
                else // CSV par défaut
                {
                    using (var memoryStream = new MemoryStream())
                    using (var writer = new StreamWriter(memoryStream))
                    {
                        // En-tête CSV
                        writer.WriteLine("Date,Downloads,UniqueUsers,TotalSizeBytes");
                        
                        // Générer des données fictives temporairement
                        var currentDate = startDate;
                        var random = new Random();
                        
                        while (currentDate <= endDate)
                        {
                            int downloads = random.Next(1, 100);
                            int uniqueUsers = random.Next(1, downloads);
                            long size = downloads * 1024 * 1024; // En octets
                            
                            writer.WriteLine($"{currentDate:yyyy-MM-dd},{downloads},{uniqueUsers},{size}");
                            currentDate = currentDate.AddDays(1);
                        }
                        
                        writer.Flush();
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating download report for mod {modId}");
                return Encoding.UTF8.GetBytes($"Error: {ex.Message}");
            }
        }
        
        #region Helpers
        
        /// <summary>
        /// Anonymise une adresse IP pour la conformité RGPD
        /// </summary>
        private string AnonymizeIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return "unknown";
            
            // Pour IPv4, on cache le dernier octet
            if (ip.Contains('.'))
            {
                var parts = ip.Split('.');
                if (parts.Length >= 4)
                {
                    return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
                }
            }
            // Pour IPv6, on cache la seconde moitié
            else if (ip.Contains(':'))
            {
                var parts = ip.Split(':');
                if (parts.Length > 4)
                {
                    return $"{parts[0]}:{parts[1]}:{parts[2]}:{parts[3]}:*:*:*:*";
                }
            }
            
            return "anonymized";
        }
        
        /// <summary>
        /// Détecte le type d'appareil à partir du User-Agent
        /// </summary>
        private string GetDeviceType(HttpContext context)
        {
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();
            
            if (userAgent.Contains("mobile") || userAgent.Contains("android") || userAgent.Contains("iphone"))
                return "Mobile";
            else if (userAgent.Contains("tablet") || userAgent.Contains("ipad"))
                return "Tablet";
            else
                return "Desktop";
        }
        
        /// <summary>
        /// Extrait le pays des en-têtes HTTP
        /// </summary>
        private string GetCountryFromHeaders(HttpContext context)
        {
            // Utiliser l'en-tête CF-IPCountry si disponible (Cloudflare)
            if (context.Request.Headers.TryGetValue("CF-IPCountry", out var cfCountry))
            {
                return cfCountry.ToString();
            }
            
            // Utiliser l'en-tête Accept-Language pour une estimation basique
            if (context.Request.Headers.TryGetValue("Accept-Language", out var languages))
            {
                var firstLanguage = languages.ToString().Split(',').FirstOrDefault();
                if (firstLanguage != null)
                {
                    var parts = firstLanguage.Split(';')[0].Split('-');
                    if (parts.Length > 1)
                    {
                        return parts[1].ToUpper(); // Code pays ISO
                    }
                }
            }
            
            return "Unknown";
        }
        
        /// <summary>
        /// Calcule la prochaine date de réinitialisation en fonction du type de quota
        /// </summary>
        private DateTime CalculateNextReset(QuotaType quotaType)
        {
            var now = DateTime.UtcNow;
            
            return quotaType switch
            {
                QuotaType.Daily => now.Date.AddDays(1),
                QuotaType.Weekly => now.Date.AddDays(7 - (int)now.DayOfWeek),
                QuotaType.Monthly => new DateTime(now.Year, now.Month, 1).AddMonths(1),
                _ => now.Date.AddDays(1) // Par défaut, réinitialisation quotidienne
            };
        }
        
        #endregion
    }
}

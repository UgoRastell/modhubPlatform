using Frontend.Models.Moderation;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Frontend.Services.Moderation.MongoDB
{
    public class ModerationMongoDBService
    {
        private readonly IMongoCollection<ContentReportDocument> _reportsCollection;
        private readonly ILogger<ModerationMongoDBService> _logger;
        private readonly Random _random = new Random();

        public ModerationMongoDBService(IOptions<MongoDBSettings> mongoDBSettings, ILogger<ModerationMongoDBService> logger)
        {
            var mongoClient = new MongoClient(mongoDBSettings.Value.ConnectionString);
            var mongoDatabase = mongoClient.GetDatabase(mongoDBSettings.Value.DatabaseName);
            _reportsCollection = mongoDatabase.GetCollection<ContentReportDocument>(mongoDBSettings.Value.ModerationCollectionName);
            _logger = logger;
        }

        /// <summary>
        /// Obtient un rapport de modération par son ID
        /// </summary>
        public async Task<ContentReport> GetReportByIdAsync(string reportId)
        {
            try
            {
                var filter = Builders<ContentReportDocument>.Filter.Eq(r => r.Id, reportId);
                var report = await _reportsCollection.Find(filter).FirstOrDefaultAsync();
                return report?.ToContentReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport {ReportId} depuis MongoDB", reportId);
                throw;
            }
        }

        /// <summary>
        /// Récupère les rapports de modération avec pagination et filtres
        /// </summary>
        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetReportsAsync(
            ReportStatus? status = null, 
            ContentType? contentType = null, 
            ReportPriority? priority = null,
            int page = 1, 
            int pageSize = 20)
        {
            try
            {
                // Construction des filtres
                var filterBuilder = Builders<ContentReportDocument>.Filter;
                var filter = filterBuilder.Empty;

                if (status.HasValue)
                {
                    filter = filter & filterBuilder.Eq(r => r.Status, status.Value);
                }

                if (contentType.HasValue)
                {
                    filter = filter & filterBuilder.Eq(r => r.ContentType, contentType.Value);
                }

                if (priority.HasValue)
                {
                    filter = filter & filterBuilder.Eq(r => r.Priority, priority.Value);
                }

                // Compter le nombre total de documents
                var totalCount = await _reportsCollection.CountDocumentsAsync(filter);
                
                // Calculer le nombre total de pages
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                // Récupérer les rapports paginés
                var reports = await _reportsCollection.Find(filter)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .SortByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Convertir en ContentReport
                var contentReports = reports.Select(r => r.ToContentReport()).ToList();
                
                return (contentReports, (int)totalCount, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rapports depuis MongoDB");
                throw;
            }
        }

        /// <summary>
        /// Récupère les rapports de modération pour un utilisateur spécifique
        /// </summary>
        public async Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetUserReportsAsync(
            string userId, 
            int page = 1, 
            int pageSize = 20)
        {
            try
            {
                var filter = Builders<ContentReportDocument>.Filter.Eq(r => r.ReportedByUserId, userId);
                
                var totalCount = await _reportsCollection.CountDocumentsAsync(filter);
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                
                var reports = await _reportsCollection.Find(filter)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .SortByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var contentReports = reports.Select(r => r.ToContentReport()).ToList();
                
                return (contentReports, (int)totalCount, totalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des rapports utilisateur depuis MongoDB");
                throw;
            }
        }

        /// <summary>
        /// Crée un nouveau rapport de modération
        /// </summary>
        public async Task<ContentReport> CreateReportAsync(CreateReportRequest request)
        {
            try
            {
                // Créer un nouveau rapport
                var newReport = new ContentReport
                {
                    ContentType = request.ContentType,
                    ContentId = request.ContentId,
                    ContentUrl = request.ContentUrl,
                    ContentSnippet = request.ContentSnippet,
                    ReportedByUserId = request.UserId,
                    ReportedByUsername = request.Username,
                    ContentCreatorUserId = request.ContentCreatorId,
                    ContentCreatorUsername = request.ContentCreatorName,
                    Reason = request.Reason,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    Status = ReportStatus.Pending,
                    Priority = ReportPriority.Medium
                };

                // Convertir en document MongoDB
                var document = ContentReportDocument.FromContentReport(newReport);
                
                // Insérer dans la base de données
                await _reportsCollection.InsertOneAsync(document);
                
                // Récupérer le rapport avec l'ID généré
                return document.ToContentReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du rapport dans MongoDB");
                throw;
            }
        }

        /// <summary>
        /// Met à jour le statut d'un rapport
        /// </summary>
        public async Task UpdateReportStatusAsync(string reportId, UpdateReportStatusRequest request)
        {
            try
            {
                var filter = Builders<ContentReportDocument>.Filter.Eq(r => r.Id, reportId);
                var update = Builders<ContentReportDocument>.Update
                    .Set(r => r.Status, request.Status)
                    .Set(r => r.StatusUpdatedAt, DateTime.UtcNow)
                    .Set(r => r.ModeratorUserId, request.ModeratorId)
                    .Set(r => r.ModeratorUsername, request.ModeratorName)
                    .Set(r => r.ModeratorNotes, request.Notes);

                await _reportsCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du rapport {ReportId} dans MongoDB", reportId);
                throw;
            }
        }

        /// <summary>
        /// Met à jour la priorité d'un rapport
        /// </summary>
        public async Task UpdateReportPriorityAsync(string reportId, UpdateReportPriorityRequest request)
        {
            try
            {
                var filter = Builders<ContentReportDocument>.Filter.Eq(r => r.Id, reportId);
                var update = Builders<ContentReportDocument>.Update
                    .Set(r => r.Priority, request.Priority);

                await _reportsCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la priorité du rapport {ReportId} dans MongoDB", reportId);
                throw;
            }
        }

        /// <summary>
        /// Prend une action de modération sur un rapport
        /// </summary>
        public async Task TakeModeratorActionAsync(string reportId, ModeratorActionRequest request)
        {
            try
            {
                // Dans un scénario réel, cette méthode pourrait effectuer des actions supplémentaires
                // comme bannir l'utilisateur, supprimer du contenu, etc.
                
                // Pour l'instant, on met simplement à jour le statut et les notes
                var filter = Builders<ContentReportDocument>.Filter.Eq(r => r.Id, reportId);
                var update = Builders<ContentReportDocument>.Update
                    .Set(r => r.Status, ReportStatus.Resolved)
                    .Set(r => r.StatusUpdatedAt, DateTime.UtcNow)
                    .Set(r => r.ModeratorUserId, request.ModeratorId)
                    .Set(r => r.ModeratorUsername, request.ModeratorName)
                    .Set(r => r.ModeratorNotes, request.ActionNotes);

                await _reportsCollection.UpdateOneAsync(filter, update);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prise d'action sur le rapport {ReportId} dans MongoDB", reportId);
                throw;
            }
        }

        /// <summary>
        /// Récupère des statistiques de modération
        /// </summary>
        public async Task<ModerationStatistics> GetModerationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var filterBuilder = Builders<ContentReportDocument>.Filter;
                var filter = filterBuilder.Empty;

                // Appliquer les filtres de date si spécifiés
                if (startDate.HasValue)
                {
                    filter = filter & filterBuilder.Gte(r => r.CreatedAt, startDate.Value);
                }

                if (endDate.HasValue)
                {
                    filter = filter & filterBuilder.Lte(r => r.CreatedAt, endDate.Value);
                }

                // Compter le nombre total de rapports
                var totalReports = await _reportsCollection.CountDocumentsAsync(filter);

                // Compter les rapports en attente
                var pendingFilter = filter & filterBuilder.Eq(r => r.Status, ReportStatus.Pending);
                var pendingReports = await _reportsCollection.CountDocumentsAsync(pendingFilter);

                // Compter les rapports résolus
                var resolvedFilter = filter & filterBuilder.Eq(r => r.Status, ReportStatus.Resolved);
                var resolvedReports = await _reportsCollection.CountDocumentsAsync(resolvedFilter);

                // Compter les rapports rejetés
                var rejectedFilter = filter & filterBuilder.Eq(r => r.Status, ReportStatus.Rejected);
                var rejectedReports = await _reportsCollection.CountDocumentsAsync(rejectedFilter);

                // Compter par type de contenu
                var contentTypeCounts = new Dictionary<ContentType, int>();
                foreach (ContentType contentType in Enum.GetValues(typeof(ContentType)))
                {
                    var contentTypeFilter = filter & filterBuilder.Eq(r => r.ContentType, contentType);
                    var count = await _reportsCollection.CountDocumentsAsync(contentTypeFilter);
                    contentTypeCounts[contentType] = (int)count;
                }

                return new ModerationStatistics
                {
                    TotalReports = (int)totalReports,
                    PendingReports = (int)pendingReports,
                    ResolvedReports = (int)resolvedReports,
                    RejectedReports = (int)rejectedReports,
                    ContentTypeCounts = contentTypeCounts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de modération depuis MongoDB");
                throw;
            }
        }

        /// <summary>
        /// Initialise la collection avec des données mock si elle est vide
        /// </summary>
        public async Task InitializeMockDataAsync()
        {
            try
            {
                // Vérifier si la collection est vide
                var count = await _reportsCollection.CountDocumentsAsync(FilterDefinition<ContentReportDocument>.Empty);
                
                if (count == 0)
                {
                    _logger.LogInformation("Initialisation des données mock pour la modération...");
                    
                    // Créer une liste de rapports fictifs
                    var mockReports = new List<ContentReportDocument>();
                    
                    for (int i = 1; i <= 100; i++)
                    {
                        var reportStatus = (ReportStatus)(_random.Next(5));
                        var reportContentType = (ContentType)(_random.Next(8));
                        var reportPriority = (ReportPriority)(_random.Next(4));
                        
                        var report = new ContentReportDocument
                        {
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
                        
                        mockReports.Add(report);
                    }
                    
                    // Insérer les données mock dans MongoDB
                    await _reportsCollection.InsertManyAsync(mockReports);
                    
                    _logger.LogInformation($"{mockReports.Count} rapports mock insérés dans MongoDB");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des données mock dans MongoDB");
                throw;
            }
        }
    }
}

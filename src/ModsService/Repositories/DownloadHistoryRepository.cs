using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ModsService.Models;
using System.Linq;

namespace ModsService.Repositories
{
    /// <summary>
    /// Implémentation MongoDB du repository de l'historique des téléchargements
    /// </summary>
    public class DownloadHistoryRepository : IDownloadHistoryRepository
    {
        private readonly IMongoCollection<DownloadHistory> _downloadHistory;
        private readonly IMongoCollection<Mod> _mods;
        private readonly ILogger<DownloadHistoryRepository> _logger;
        
        public DownloadHistoryRepository(
            IMongoDatabase database, 
            ILogger<DownloadHistoryRepository> logger)
        {
            _downloadHistory = database.GetCollection<DownloadHistory>("DownloadHistory");
            _mods = database.GetCollection<Mod>("Mods");
            _logger = logger;
            
            // Créer des index
            CreateIndexes();
        }
        
        private void CreateIndexes()
        {
            try
            {
                // Index sur l'ID du mod pour recherche rapide de l'historique d'un mod
                var modIdIndex = Builders<DownloadHistory>.IndexKeys.Ascending(h => h.ModId);
                
                // Index sur l'ID de l'utilisateur pour recherche rapide de l'historique d'un utilisateur
                var userIdIndex = Builders<DownloadHistory>.IndexKeys.Ascending(h => h.UserId);
                
                // Index sur la date de téléchargement pour recherche par plage de dates
                var downloadDateIndex = Builders<DownloadHistory>.IndexKeys.Descending(h => h.DownloadedAt);
                
                // Index composé pour recherche par mod et date
                var modDateIndex = Builders<DownloadHistory>.IndexKeys
                    .Ascending(h => h.ModId)
                    .Descending(h => h.DownloadedAt);
                    
                // Index composé pour recherche par utilisateur et date
                var userDateIndex = Builders<DownloadHistory>.IndexKeys
                    .Ascending(h => h.UserId)
                    .Descending(h => h.DownloadedAt);
                    
                // Index composé pour recherche par mod et version
                var modVersionIndex = Builders<DownloadHistory>.IndexKeys
                    .Ascending(h => h.ModId)
                    .Ascending(h => h.VersionNumber);
                    
                // Index pour recherche par pays (pour les statistiques géographiques)
                var countryIndex = Builders<DownloadHistory>.IndexKeys.Ascending(h => h.Country);
                
                // Index sur le type d'appareil (pour les statistiques par appareil)
                var deviceTypeIndex = Builders<DownloadHistory>.IndexKeys.Ascending(h => h.DeviceType);
                
                // Index TTL pour supprimer automatiquement les anciens enregistrements (conservation 1 an par défaut)
                var ttlIndex = Builders<DownloadHistory>.IndexKeys.Ascending(h => h.DownloadedAt);
                var ttlOptions = new CreateIndexOptions 
                { 
                    Name = "ttl_downloadedAt",
                    ExpireAfter = TimeSpan.FromDays(365) // 1 an de rétention par défaut
                };
                
                // Créer les index s'ils n'existent pas déjà
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(modIdIndex, 
                    new CreateIndexOptions { Name = "ix_modId" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(userIdIndex, 
                    new CreateIndexOptions { Name = "ix_userId" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(downloadDateIndex, 
                    new CreateIndexOptions { Name = "ix_downloadedAt" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(modDateIndex, 
                    new CreateIndexOptions { Name = "ix_modId_downloadedAt" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(userDateIndex, 
                    new CreateIndexOptions { Name = "ix_userId_downloadedAt" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(modVersionIndex, 
                    new CreateIndexOptions { Name = "ix_modId_versionNumber" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(countryIndex, 
                    new CreateIndexOptions { Name = "ix_country" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(deviceTypeIndex, 
                    new CreateIndexOptions { Name = "ix_deviceType" }));
                _downloadHistory.Indexes.CreateOne(new CreateIndexModel<DownloadHistory>(ttlIndex, ttlOptions));
                
                _logger.LogInformation("MongoDB indexes created successfully for DownloadHistory collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MongoDB indexes for DownloadHistory collection");
            }
        }
        
        /// <inheritdoc />
        public async Task<DownloadHistory> AddDownloadRecordAsync(DownloadHistory downloadRecord)
        {
            try
            {
                // Ajouter l'enregistrement à la collection
                await _downloadHistory.InsertOneAsync(downloadRecord);
                
                // Mettre à jour les compteurs de téléchargement du mod
                await UpdateModDownloadCountersAsync(downloadRecord.ModId, downloadRecord.VersionNumber);
                
                return downloadRecord;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding download record for mod {downloadRecord.ModId}");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> UpdateDownloadRecordAsync(DownloadHistory downloadRecord)
        {
            try
            {
                // Mettre à jour l'enregistrement
                var result = await _downloadHistory.ReplaceOneAsync(
                    h => h.Id == downloadRecord.Id, 
                    downloadRecord);
                    
                return result.IsAcknowledged && result.ModifiedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating download record {downloadRecord.Id}");
                return false;
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<DownloadHistory>> GetModDownloadHistoryAsync(
            string modId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int skip = 0, 
            int limit = 50)
        {
            try
            {
                // Construire le filtre
                var filterBuilder = Builders<DownloadHistory>.Filter;
                var filter = filterBuilder.Eq(h => h.ModId, modId);
                
                if (startDate.HasValue)
                {
                    filter &= filterBuilder.Gte(h => h.DownloadedAt, startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    filter &= filterBuilder.Lte(h => h.DownloadedAt, endDate.Value);
                }
                
                // Exécuter la requête avec pagination
                return await _downloadHistory
                    .Find(filter)
                    .Sort(Builders<DownloadHistory>.Sort.Descending(h => h.DownloadedAt))
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving download history for mod {modId}");
                return new List<DownloadHistory>();
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<DownloadHistory>> GetUserDownloadHistoryAsync(
            string userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int skip = 0, 
            int limit = 50)
        {
            try
            {
                // Construire le filtre
                var filterBuilder = Builders<DownloadHistory>.Filter;
                var filter = filterBuilder.Eq(h => h.UserId, userId);
                
                if (startDate.HasValue)
                {
                    filter &= filterBuilder.Gte(h => h.DownloadedAt, startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    filter &= filterBuilder.Lte(h => h.DownloadedAt, endDate.Value);
                }
                
                // Exécuter la requête avec pagination
                return await _downloadHistory
                    .Find(filter)
                    .Sort(Builders<DownloadHistory>.Sort.Descending(h => h.DownloadedAt))
                    .Skip(skip)
                    .Limit(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving download history for user {userId}");
                return new List<DownloadHistory>();
            }
        }
        
        /// <inheritdoc />
        public async Task<Dictionary<string, long>> GetDownloadCountByVersionAsync(string modId)
        {
            try
            {
                // Utiliser l'agrégation MongoDB pour compter les téléchargements par version
                var pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument("ModId", modId)),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", "$VersionNumber" },
                        { "count", new BsonDocument("$sum", 1) }
                    })
                };
                
                var results = await _downloadHistory.Aggregate<BsonDocument>(pipeline).ToListAsync();
                
                var downloadCounts = new Dictionary<string, long>();
                foreach (var result in results)
                {
                    downloadCounts[result["_id"].AsString] = result["count"].AsInt64;
                }
                
                return downloadCounts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting download count by version for mod {modId}");
                return new Dictionary<string, long>();
            }
        }
        
        /// <inheritdoc />
        public async Task<Dictionary<DateTime, long>> GetDailyDownloadStatsAsync(
            string modId, 
            DateTime startDate, 
            DateTime endDate)
        {
            try
            {
                // Convertir les dates en début et fin de jour
                var start = startDate.Date;
                var end = endDate.Date.AddDays(1).AddTicks(-1);
                
                // Construire le pipeline d'agrégation
                var pipeline = new[]
                {
                    new BsonDocument("$match", new BsonDocument
                    {
                        { "ModId", modId },
                        { "DownloadedAt", new BsonDocument
                            {
                                { "$gte", start },
                                { "$lte", end }
                            }
                        }
                    }),
                    new BsonDocument("$group", new BsonDocument
                    {
                        { "_id", new BsonDocument("$dateToString", new BsonDocument
                            {
                                { "format", "%Y-%m-%d" },
                                { "date", "$DownloadedAt" }
                            })
                        },
                        { "count", new BsonDocument("$sum", 1) }
                    }),
                    new BsonDocument("$sort", new BsonDocument("_id", 1))
                };
                
                var results = await _downloadHistory.Aggregate<BsonDocument>(pipeline).ToListAsync();
                
                var dailyStats = new Dictionary<DateTime, long>();
                
                // Remplir toutes les dates dans la plage demandée avec 0 par défaut
                for (var day = start; day <= end; day = day.AddDays(1))
                {
                    dailyStats[day] = 0;
                }
                
                // Mettre à jour avec les résultats réels
                foreach (var result in results)
                {
                    var dateStr = result["_id"].AsString;
                    var date = DateTime.ParseExact(dateStr, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    dailyStats[date] = result["count"].AsInt64;
                }
                
                return dailyStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting daily download stats for mod {modId}");
                return new Dictionary<DateTime, long>();
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> HasUserRecentlyDownloadedModAsync(
            string userId, 
            string modId, 
            string versionId = null,
            TimeSpan? withinTimeFrame = null)
        {
            try
            {
                // Si aucune période n'est spécifiée, utiliser 24 heures par défaut
                withinTimeFrame ??= TimeSpan.FromHours(24);
                
                // Calculer la date limite
                var cutoffDate = DateTime.UtcNow.Subtract(withinTimeFrame.Value);
                
                // Construire le filtre
                var filterBuilder = Builders<DownloadHistory>.Filter;
                var filter = filterBuilder.Eq(h => h.UserId, userId) &
                             filterBuilder.Eq(h => h.ModId, modId) &
                             filterBuilder.Gte(h => h.DownloadedAt, cutoffDate);
                
                if (!string.IsNullOrEmpty(versionId))
                {
                    filter &= filterBuilder.Eq(h => h.VersionId, versionId);
                }
                
                // Vérifier s'il existe au moins un enregistrement correspondant
                var count = await _downloadHistory.CountDocumentsAsync(filter);
                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking recent downloads for user {userId} and mod {modId}");
                return false;
            }
        }
        
        /// <inheritdoc />
        public async Task CleanupHistoricalDataAsync(TimeSpan retentionPeriod)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.Subtract(retentionPeriod);
                
                var filter = Builders<DownloadHistory>.Filter.Lt(h => h.DownloadedAt, cutoffDate);
                var result = await _downloadHistory.DeleteManyAsync(filter);
                
                _logger.LogInformation($"Cleaned up {result.DeletedCount} historical download records older than {cutoffDate}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up historical download data");
            }
        }
        
        /// <summary>
        /// Met à jour les compteurs de téléchargement pour un mod et sa version
        /// </summary>
        private async Task UpdateModDownloadCountersAsync(string modId, string versionNumber)
        {
            try
            {
                // Mettre à jour le compteur global du mod
                var modUpdate = Builders<Mod>.Update.Inc(m => m.DownloadCount, 1);
                
                // Mettre à jour le compteur de la version spécifique
                var versionFilter = Builders<Mod>.Filter.Eq(m => m.Id, modId) &
                                    Builders<Mod>.Filter.ElemMatch(m => m.Versions, v => v.VersionNumber == versionNumber);
                var versionUpdate = Builders<Mod>.Update.Inc("Versions.$.DownloadCount", 1);
                
                // Exécuter les mises à jour
                await _mods.UpdateOneAsync(m => m.Id == modId, modUpdate);
                await _mods.UpdateOneAsync(versionFilter, versionUpdate);
                
                // Mettre à jour le dictionnaire de téléchargements par version
                var mod = await _mods.Find(m => m.Id == modId).FirstOrDefaultAsync();
                if (mod != null)
                {
                    // Initialiser le dictionnaire s'il n'existe pas
                    mod.DownloadsByVersion ??= new Dictionary<string, long>();
                    
                    // Mettre à jour le compteur de la version
                    if (mod.DownloadsByVersion.ContainsKey(versionNumber))
                    {
                        mod.DownloadsByVersion[versionNumber]++;
                    }
                    else
                    {
                        mod.DownloadsByVersion[versionNumber] = 1;
                    }
                    
                    await _mods.ReplaceOneAsync(m => m.Id == modId, mod);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating download counters for mod {modId}, version {versionNumber}");
            }
        }
    }
}

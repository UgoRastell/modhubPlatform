using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ModsService.Models;

namespace ModsService.Repositories
{
    /// <summary>
    /// Implémentation MongoDB du repository des quotas de téléchargement
    /// </summary>
    public class DownloadQuotaRepository : IDownloadQuotaRepository
    {
        private readonly IMongoCollection<DownloadQuota> _downloadQuotas;
        private readonly ILogger<DownloadQuotaRepository> _logger;
        
        public DownloadQuotaRepository(IMongoDatabase database, ILogger<DownloadQuotaRepository> logger)
        {
            _downloadQuotas = database.GetCollection<DownloadQuota>("DownloadQuotas");
            _logger = logger;
            
            // Créer des index
            CreateIndexes();
        }
        
        private void CreateIndexes()
        {
            try
            {
                // Index sur l'ID utilisateur pour recherche rapide des quotas d'un utilisateur
                var userIdIndex = Builders<DownloadQuota>.IndexKeys.Ascending(q => q.UserId);
                
                // Index composé sur l'ID utilisateur + type de quota pour recherche spécifique
                var userIdTypeIndex = Builders<DownloadQuota>.IndexKeys
                    .Ascending(q => q.UserId)
                    .Ascending(q => q.Type);
                    
                // Index sur la date de réinitialisation pour trouver rapidement les quotas expirés
                var nextResetIndex = Builders<DownloadQuota>.IndexKeys.Ascending(q => q.NextReset);
                
                // Créer les index s'ils n'existent pas déjà
                _downloadQuotas.Indexes.CreateOne(new CreateIndexModel<DownloadQuota>(userIdIndex, 
                    new CreateIndexOptions { Name = "ix_userId" }));
                _downloadQuotas.Indexes.CreateOne(new CreateIndexModel<DownloadQuota>(userIdTypeIndex, 
                    new CreateIndexOptions { Name = "ix_userId_type", Unique = true }));
                _downloadQuotas.Indexes.CreateOne(new CreateIndexModel<DownloadQuota>(nextResetIndex, 
                    new CreateIndexOptions { Name = "ix_nextReset" }));
                
                _logger.LogInformation("MongoDB indexes created successfully for DownloadQuotas collection");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MongoDB indexes for DownloadQuotas collection");
            }
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<DownloadQuota>> GetQuotasByUserIdAsync(string userId)
        {
            try
            {
                return await _downloadQuotas.Find(q => q.UserId == userId).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quotas for user {userId}");
                return new List<DownloadQuota>();
            }
        }
        
        /// <inheritdoc />
        public async Task<DownloadQuota> GetQuotaByTypeAsync(string userId, QuotaType quotaType)
        {
            try
            {
                return await _downloadQuotas
                    .Find(q => q.UserId == userId && q.Type == quotaType)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving {quotaType} quota for user {userId}");
                return null;
            }
        }
        
        /// <inheritdoc />
        public async Task<DownloadQuota> UpsertQuotaAsync(DownloadQuota quota)
        {
            try
            {
                // Vérifier si le quota existe déjà
                var existingQuota = await _downloadQuotas
                    .Find(q => q.UserId == quota.UserId && q.Type == quota.Type)
                    .FirstOrDefaultAsync();
                    
                if (existingQuota != null)
                {
                    // Mettre à jour le quota existant
                    quota.Id = existingQuota.Id;
                    await _downloadQuotas.ReplaceOneAsync(q => q.Id == existingQuota.Id, quota);
                }
                else
                {
                    // Créer un nouveau quota
                    await _downloadQuotas.InsertOneAsync(quota);
                }
                
                return quota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error upserting quota for user {quota.UserId}");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task<DownloadQuota> IncrementQuotaUsageAsync(string userId, QuotaType quotaType, int incrementBy = 1)
        {
            try
            {
                var quota = await GetQuotaByTypeAsync(userId, quotaType);
                
                // Si le quota n'existe pas, créer un nouveau avec les valeurs par défaut
                if (quota == null)
                {
                    quota = CreateDefaultQuota(userId, quotaType);
                }
                else
                {
                    // Vérifier si le quota doit être réinitialisé
                    if (DateTime.UtcNow >= quota.NextReset)
                    {
                        await ResetQuotaAsync(quota);
                    }
                }
                
                // Incrémenter l'utilisation
                var update = Builders<DownloadQuota>.Update.Inc(q => q.CurrentUsage, incrementBy);
                var options = new FindOneAndUpdateOptions<DownloadQuota> { ReturnDocument = ReturnDocument.After };
                
                quota = await _downloadQuotas.FindOneAndUpdateAsync(
                    q => q.Id == quota.Id, 
                    update, 
                    options);
                    
                return quota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error incrementing {quotaType} quota for user {userId}");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task ResetExpiredQuotasAsync()
        {
            try
            {
                var expiredQuotas = await _downloadQuotas
                    .Find(q => q.NextReset <= DateTime.UtcNow)
                    .ToListAsync();
                    
                foreach (var quota in expiredQuotas)
                {
                    await ResetQuotaAsync(quota);
                }
                
                _logger.LogInformation($"Reset {expiredQuotas.Count} expired quotas");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting expired quotas");
            }
        }
        
        /// <inheritdoc />
        public async Task<bool> HasExceededQuotaAsync(string userId, QuotaType quotaType)
        {
            try
            {
                var quota = await GetQuotaByTypeAsync(userId, quotaType);
                
                // Si le quota n'existe pas, créer un nouveau avec les valeurs par défaut
                if (quota == null)
                {
                    quota = CreateDefaultQuota(userId, quotaType);
                    await UpsertQuotaAsync(quota);
                }
                else
                {
                    // Vérifier si le quota doit être réinitialisé
                    if (DateTime.UtcNow >= quota.NextReset)
                    {
                        await ResetQuotaAsync(quota);
                        return false; // Le quota vient d'être réinitialisé, donc non dépassé
                    }
                }
                
                return quota.HasExceededQuota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if user {userId} has exceeded {quotaType} quota");
                return false; // En cas d'erreur, on suppose que le quota n'est pas dépassé
            }
        }
        
        /// <inheritdoc />
        public async Task<int> GetRemainingQuotaAsync(string userId, QuotaType quotaType)
        {
            try
            {
                var quota = await GetQuotaByTypeAsync(userId, quotaType);
                
                // Si le quota n'existe pas, créer un nouveau avec les valeurs par défaut
                if (quota == null)
                {
                    quota = CreateDefaultQuota(userId, quotaType);
                    await UpsertQuotaAsync(quota);
                }
                else
                {
                    // Vérifier si le quota doit être réinitialisé
                    if (DateTime.UtcNow >= quota.NextReset)
                    {
                        await ResetQuotaAsync(quota);
                    }
                }
                
                return quota.RemainingQuota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting remaining quota for user {userId}");
                
                // En cas d'erreur, retourner une valeur par défaut basée sur le type de quota
                return GetDefaultQuotaLimit(quotaType);
            }
        }
        
        /// <summary>
        /// Réinitialise un quota spécifique
        /// </summary>
        private async Task<DownloadQuota> ResetQuotaAsync(DownloadQuota quota)
        {
            try
            {
                // Mettre à jour les dates de réinitialisation
                quota.LastReset = DateTime.UtcNow;
                quota.NextReset = CalculateNextReset(quota.Type);
                
                // Réinitialiser l'utilisation
                quota.CurrentUsage = 0;
                
                // Mettre à jour en base de données
                await _downloadQuotas.ReplaceOneAsync(q => q.Id == quota.Id, quota);
                
                return quota;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting quota {quota.Id}");
                throw;
            }
        }
        
        /// <summary>
        /// Crée un quota par défaut pour un utilisateur et un type donnés
        /// </summary>
        private DownloadQuota CreateDefaultQuota(string userId, QuotaType quotaType)
        {
            var now = DateTime.UtcNow;
            
            return new DownloadQuota
            {
                UserId = userId,
                Type = quotaType,
                Limit = GetDefaultQuotaLimit(quotaType),
                CurrentUsage = 0,
                LastReset = now,
                NextReset = CalculateNextReset(quotaType),
                IsBlocking = true // Par défaut, les quotas sont bloquants
            };
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
        
        /// <summary>
        /// Retourne la limite de quota par défaut en fonction du type
        /// </summary>
        private int GetDefaultQuotaLimit(QuotaType quotaType)
        {
            return quotaType switch
            {
                QuotaType.Daily => 20,    // 20 téléchargements par jour
                QuotaType.Weekly => 100,  // 100 téléchargements par semaine
                QuotaType.Monthly => 500, // 500 téléchargements par mois
                _ => 20                   // Par défaut, 20 téléchargements
            };
        }
    }
}

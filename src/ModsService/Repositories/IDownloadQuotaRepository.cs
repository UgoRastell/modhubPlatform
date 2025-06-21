using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;

namespace ModsService.Repositories
{
    /// <summary>
    /// Interface pour le repository des quotas de téléchargement
    /// </summary>
    public interface IDownloadQuotaRepository
    {
        /// <summary>
        /// Récupère tous les quotas d'un utilisateur
        /// </summary>
        Task<IEnumerable<DownloadQuota>> GetQuotasByUserIdAsync(string userId);
        
        /// <summary>
        /// Récupère un quota spécifique pour un utilisateur
        /// </summary>
        Task<DownloadQuota> GetQuotaByTypeAsync(string userId, QuotaType quotaType);
        
        /// <summary>
        /// Crée ou met à jour un quota de téléchargement
        /// </summary>
        Task<DownloadQuota> UpsertQuotaAsync(DownloadQuota quota);
        
        /// <summary>
        /// Incrémente l'utilisation d'un quota
        /// </summary>
        Task<DownloadQuota> IncrementQuotaUsageAsync(string userId, QuotaType quotaType, int incrementBy = 1);
        
        /// <summary>
        /// Réinitialise les quotas expirés
        /// </summary>
        Task ResetExpiredQuotasAsync();
        
        /// <summary>
        /// Vérifie si un utilisateur a dépassé son quota
        /// </summary>
        Task<bool> HasExceededQuotaAsync(string userId, QuotaType quotaType);
        
        /// <summary>
        /// Récupère le quota restant pour un utilisateur
        /// </summary>
        Task<int> GetRemainingQuotaAsync(string userId, QuotaType quotaType);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;

namespace ModsService.Repositories
{
    /// <summary>
    /// Interface pour le repository de l'historique des téléchargements
    /// </summary>
    public interface IDownloadHistoryRepository
    {
        /// <summary>
        /// Ajoute un nouvel enregistrement à l'historique des téléchargements
        /// </summary>
        Task<DownloadHistory> AddDownloadRecordAsync(DownloadHistory downloadRecord);
        
        /// <summary>
        /// Met à jour un enregistrement existant
        /// </summary>
        Task<bool> UpdateDownloadRecordAsync(DownloadHistory downloadRecord);
        
        /// <summary>
        /// Récupère l'historique des téléchargements d'un mod
        /// </summary>
        Task<IEnumerable<DownloadHistory>> GetModDownloadHistoryAsync(
            string modId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int skip = 0, 
            int limit = 50);
        
        /// <summary>
        /// Récupère l'historique des téléchargements d'un utilisateur
        /// </summary>
        Task<IEnumerable<DownloadHistory>> GetUserDownloadHistoryAsync(
            string userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            int skip = 0, 
            int limit = 50);
        
        /// <summary>
        /// Récupère les statistiques de téléchargement d'un mod
        /// </summary>
        Task<DownloadStatistics> GetModDownloadStatisticsAsync(
            string modId, 
            DateTime? startDate = null, 
            DateTime? endDate = null);
        
        /// <summary>
        /// Calcule les statistiques globales de téléchargement
        /// </summary>
        Task<DownloadStatistics> GetGlobalDownloadStatisticsAsync(
            DateTime? startDate = null, 
            DateTime? endDate = null);
        
        /// <summary>
        /// Récupère le nombre de téléchargements par version pour un mod
        /// </summary>
        Task<Dictionary<string, long>> GetDownloadCountByVersionAsync(string modId);
        
        /// <summary>
        /// Récupère le nombre de téléchargements par jour pour un mod
        /// </summary>
        Task<Dictionary<DateTime, long>> GetDailyDownloadStatsAsync(
            string modId, 
            DateTime startDate, 
            DateTime endDate);
            
        /// <summary>
        /// Vérifie si un utilisateur a récemment téléchargé un mod spécifique
        /// </summary>
        Task<bool> HasUserRecentlyDownloadedModAsync(
            string userId, 
            string modId, 
            string versionId = null,
            TimeSpan? withinTimeFrame = null);
            
        /// <summary>
        /// Nettoie les anciennes données d'historique (RGPD/optimisation)
        /// </summary>
        Task CleanupHistoricalDataAsync(TimeSpan retentionPeriod);
    }
}

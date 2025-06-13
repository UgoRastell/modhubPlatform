using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;
using Microsoft.AspNetCore.Http;

namespace ModsService.Services.Download
{
    /// <summary>
    /// Interface pour le service de téléchargement de mods
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Enregistre un téléchargement et met à jour les statistiques
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="versionNumber">Numéro de version du mod</param>
        /// <param name="userId">ID de l'utilisateur (null si anonyme)</param>
        /// <param name="httpContext">Contexte HTTP pour extraire les informations client</param>
        /// <returns>Le résultat du téléchargement</returns>
        Task<DownloadResult> RecordDownloadAsync(
            string modId, 
            string versionNumber, 
            string userId,
            HttpContext httpContext);
        
        /// <summary>
        /// Vérifie si l'utilisateur a le droit de télécharger (quotas)
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="quotaType">Type de quota à vérifier</param>
        /// <returns>Résultat de la vérification de quota</returns>
        Task<QuotaCheckResult> CheckUserQuotaAsync(string userId, QuotaType quotaType);
        
        /// <summary>
        /// Récupère les statistiques de téléchargement d'un mod
        /// </summary>
        /// <param name="modId">ID du mod</param>
        /// <param name="startDate">Date de début (optionnelle)</param>
        /// <param name="endDate">Date de fin (optionnelle)</param>
        /// <returns>Statistiques de téléchargement</returns>
        Task<DownloadStatistics> GetModDownloadStatisticsAsync(
            string modId,
            DateTime? startDate = null,
            DateTime? endDate = null);
        
        /// <summary>
        /// Récupère l'historique des téléchargements d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="page">Numéro de page</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste des téléchargements de l'utilisateur</returns>
        Task<PaginatedResult<DownloadHistory>> GetUserDownloadHistoryAsync(
            string userId,
            int page = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Génère un rapport de téléchargement
        /// </summary>
        /// <param name="modId">ID du mod (optionnel, null pour rapport global)</param>
        /// <param name="startDate">Date de début</param>
        /// <param name="endDate">Date de fin</param>
        /// <param name="format">Format du rapport (csv, json, etc.)</param>
        /// <returns>Contenu du rapport</returns>
        Task<byte[]> GenerateDownloadReportAsync(
            string modId,
            DateTime startDate,
            DateTime endDate,
            string format = "csv");
    }
    
    /// <summary>
    /// Résultat d'un téléchargement
    /// </summary>
    public class DownloadResult
    {
        /// <summary>
        /// Indique si le téléchargement est autorisé
        /// </summary>
        public bool IsAllowed { get; set; }
        
        /// <summary>
        /// Indique si le quota a été dépassé
        /// </summary>
        public bool QuotaExceeded { get; set; }
        
        /// <summary>
        /// ID de l'enregistrement du téléchargement
        /// </summary>
        public string DownloadId { get; set; }
        
        /// <summary>
        /// Quota restant après ce téléchargement
        /// </summary>
        public int RemainingQuota { get; set; }
        
        /// <summary>
        /// Message informatif (en cas d'erreur ou limitation)
        /// </summary>
        public string Message { get; set; }
    }
    
    /// <summary>
    /// Résultat de la vérification de quota
    /// </summary>
    public class QuotaCheckResult
    {
        /// <summary>
        /// Indique si le téléchargement est autorisé selon le quota
        /// </summary>
        public bool IsAllowed { get; set; }
        
        /// <summary>
        /// Quota actuel
        /// </summary>
        public int CurrentUsage { get; set; }
        
        /// <summary>
        /// Limite du quota
        /// </summary>
        public int Limit { get; set; }
        
        /// <summary>
        /// Quota restant
        /// </summary>
        public int RemainingQuota => Math.Max(0, Limit - CurrentUsage);
        
        /// <summary>
        /// Date de la prochaine réinitialisation du quota
        /// </summary>
        public DateTime NextReset { get; set; }
        
        /// <summary>
        /// Type de quota vérifié
        /// </summary>
        public QuotaType QuotaType { get; set; }
    }
    
    /// <summary>
    /// Résultat paginé générique
    /// </summary>
    public class PaginatedResult<T>
    {
        /// <summary>
        /// Items de la page courante
        /// </summary>
        public IEnumerable<T> Items { get; set; }
        
        /// <summary>
        /// Numéro de la page courante
        /// </summary>
        public int CurrentPage { get; set; }
        
        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// Nombre total d'items
        /// </summary>
        public int TotalItems { get; set; }
        
        /// <summary>
        /// Taille de la page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Indique s'il y a une page précédente
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;
        
        /// <summary>
        /// Indique s'il y a une page suivante
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

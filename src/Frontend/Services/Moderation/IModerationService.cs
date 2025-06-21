using Frontend.Models.Moderation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Moderation
{
    public interface IModerationService
    {
        /// <summary>
        /// Signale un contenu inapproprié
        /// </summary>
        /// <param name="request">La requête de signalement</param>
        /// <returns>Le signalement créé</returns>
        Task<ContentReport> ReportContentAsync(CreateReportRequest request);
        
        /// <summary>
        /// Récupère un signalement par son ID
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <returns>Le signalement ou null s'il n'est pas trouvé</returns>
        Task<ContentReport> GetReportByIdAsync(string reportId);
        
        /// <summary>
        /// Récupère les signalements de l'utilisateur actuellement connecté
        /// </summary>
        /// <param name="page">Numéro de page</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste des signalements et méta-données de pagination</returns>
        Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetMyReportsAsync(int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère tous les signalements avec filtres (pour modérateurs et administrateurs)
        /// </summary>
        /// <param name="status">Filtre par statut</param>
        /// <param name="contentType">Filtre par type de contenu</param>
        /// <param name="priority">Filtre par priorité</param>
        /// <param name="page">Numéro de page</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste des signalements et méta-données de pagination</returns>
        Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetAllReportsAsync(
            ReportStatus? status = null,
            ContentType? contentType = null,
            ReportPriority? priority = null,
            int page = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Met à jour le statut d'un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="request">Requête de mise à jour</param>
        Task UpdateReportStatusAsync(string reportId, UpdateReportStatusRequest request);
        
        /// <summary>
        /// Prend une action de modération sur un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="request">Requête d'action</param>
        Task TakeModeratorActionAsync(string reportId, ModeratorActionRequest request);
        
        /// <summary>
        /// Met à jour la priorité d'un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="request">Requête de mise à jour</param>
        Task UpdateReportPriorityAsync(string reportId, UpdateReportPriorityRequest request);
        
        /// <summary>
        /// Récupère les statistiques de modération
        /// </summary>
        /// <param name="startDate">Date de début pour la période</param>
        /// <param name="endDate">Date de fin pour la période</param>
        /// <returns>Statistiques de modération</returns>
        Task<ModerationStatistics> GetModerationStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}

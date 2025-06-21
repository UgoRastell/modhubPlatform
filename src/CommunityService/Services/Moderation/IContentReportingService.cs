using CommunityService.Models.Moderation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CommunityService.Services.Moderation
{
    public interface IContentReportingService
    {
        /// <summary>
        /// Crée un nouveau signalement de contenu
        /// </summary>
        /// <param name="report">Le signalement à créer</param>
        /// <returns>Le signalement créé avec son ID</returns>
        Task<ContentReport> CreateReportAsync(ContentReport report);

        /// <summary>
        /// Récupère un signalement par son ID
        /// </summary>
        /// <param name="reportId">ID du signalement à récupérer</param>
        /// <returns>Le signalement ou null s'il n'est pas trouvé</returns>
        Task<ContentReport?> GetReportByIdAsync(string reportId);

        /// <summary>
        /// Récupère tous les signalements avec filtrage et pagination
        /// </summary>
        /// <param name="status">Filtre optionnel par statut</param>
        /// <param name="contentType">Filtre optionnel par type de contenu</param>
        /// <param name="priority">Filtre optionnel par priorité</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste des signalements correspondant aux critères</returns>
        Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetReportsAsync(
            ReportStatus? status = null, 
            ContentType? contentType = null,
            ReportPriority? priority = null,
            int page = 1, 
            int pageSize = 20);

        /// <summary>
        /// Récupère les signalements pour un contenu spécifique
        /// </summary>
        /// <param name="contentType">Type du contenu</param>
        /// <param name="contentId">ID du contenu</param>
        /// <returns>Liste des signalements pour ce contenu</returns>
        Task<List<ContentReport>> GetReportsForContentAsync(ContentType contentType, string contentId);

        /// <summary>
        /// Récupère les signalements créés par un utilisateur spécifique
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="page">Numéro de page (commence à 1)</param>
        /// <param name="pageSize">Taille de la page</param>
        /// <returns>Liste des signalements créés par l'utilisateur</returns>
        Task<(List<ContentReport> Reports, int TotalCount, int TotalPages)> GetReportsByUserAsync(
            string userId, 
            int page = 1, 
            int pageSize = 20);

        /// <summary>
        /// Met à jour le statut d'un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="status">Nouveau statut</param>
        /// <param name="moderatorUserId">ID du modérateur effectuant la mise à jour</param>
        /// <param name="moderatorUsername">Nom d'utilisateur du modérateur</param>
        /// <param name="notes">Notes optionnelles du modérateur</param>
        /// <returns>True si la mise à jour a réussi, sinon False</returns>
        Task<bool> UpdateReportStatusAsync(
            string reportId, 
            ReportStatus status, 
            string moderatorUserId,
            string moderatorUsername,
            string? notes = null);

        /// <summary>
        /// Prend une action modératrice sur un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="action">Action à prendre</param>
        /// <param name="moderatorUserId">ID du modérateur effectuant l'action</param>
        /// <param name="moderatorUsername">Nom d'utilisateur du modérateur</param>
        /// <param name="notes">Notes optionnelles du modérateur</param>
        /// <returns>True si l'action a réussi, sinon False</returns>
        Task<bool> TakeModeratorActionAsync(
            string reportId, 
            ModeratorAction action, 
            string moderatorUserId,
            string moderatorUsername,
            string? notes = null);

        /// <summary>
        /// Met à jour la priorité d'un signalement
        /// </summary>
        /// <param name="reportId">ID du signalement</param>
        /// <param name="priority">Nouvelle priorité</param>
        /// <returns>True si la mise à jour a réussi, sinon False</returns>
        Task<bool> UpdateReportPriorityAsync(string reportId, ReportPriority priority);

        /// <summary>
        /// Vérifie si un contenu a déjà été signalé par un utilisateur spécifique
        /// </summary>
        /// <param name="contentType">Type du contenu</param>
        /// <param name="contentId">ID du contenu</param>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>True si l'utilisateur a déjà signalé ce contenu, sinon False</returns>
        Task<bool> HasUserReportedContentAsync(ContentType contentType, string contentId, string userId);

        /// <summary>
        /// Récupère des statistiques sur les signalements
        /// </summary>
        /// <param name="startDate">Date de début optionnelle pour la période</param>
        /// <param name="endDate">Date de fin optionnelle pour la période</param>
        /// <returns>Statistiques sur les signalements</returns>
        Task<ModerationStatistics> GetReportingStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    public class ModerationStatistics
    {
        public int TotalReports { get; set; }
        public int PendingReports { get; set; }
        public int ResolvedReports { get; set; }
        public int RejectedReports { get; set; }
        public int HighPriorityReports { get; set; }
        public Dictionary<ContentType, int> ReportsByContentType { get; set; } = new();
        public Dictionary<ReportReason, int> ReportsByReason { get; set; } = new();
        public Dictionary<string, int> TopReportedUsers { get; set; } = new();
        public Dictionary<string, int> TopReportingUsers { get; set; } = new();
        public double AverageResolutionTimeHours { get; set; }
    }
}

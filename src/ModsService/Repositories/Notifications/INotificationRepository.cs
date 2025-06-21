using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models.Notifications;

namespace ModsService.Repositories.Notifications
{
    public interface INotificationRepository
    {
        /// <summary>
        /// Ajoute une nouvelle notification
        /// </summary>
        Task<Notification> AddNotificationAsync(Notification notification);
        
        /// <summary>
        /// Récupère une notification par son ID
        /// </summary>
        Task<Notification> GetNotificationByIdAsync(string id);
        
        /// <summary>
        /// Récupère toutes les notifications d'un utilisateur
        /// </summary>
        Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(string userId, bool unreadOnly = false);
        
        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId);
        
        /// <summary>
        /// Marque toutes les notifications d'un utilisateur comme lues
        /// </summary>
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
        
        /// <summary>
        /// Supprime une notification
        /// </summary>
        Task<bool> DeleteNotificationAsync(string id, string userId);
        
        /// <summary>
        /// Récupère les notifications en fonction de critères spécifiques
        /// </summary>
        Task<IEnumerable<Notification>> GetNotificationsByFilterAsync(
            string userId, 
            NotificationType? type = null,
            DateTime? startDate = null, 
            DateTime? endDate = null);
    }
}

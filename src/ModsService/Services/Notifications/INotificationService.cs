using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;
using ModsService.Models.Notifications;

namespace ModsService.Services.Notifications
{
    public interface INotificationService
    {
        /// <summary>
        /// Envoie une notification aux utilisateurs abonnés à un mod lorsqu'une nouvelle version est publiée
        /// </summary>
        Task<bool> NotifyNewVersionAsync(string modId, ModVersion newVersion);
        
        /// <summary>
        /// Envoie une notification à un utilisateur spécifique
        /// </summary>
        Task<bool> NotifyUserAsync(string userId, Notification notification);
        
        /// <summary>
        /// Envoie une notification à plusieurs utilisateurs spécifiques
        /// </summary>
        Task<bool> NotifyUsersAsync(IEnumerable<string> userIds, Notification notification);
        
        /// <summary>
        /// Abonne un utilisateur aux notifications d'un mod
        /// </summary>
        Task<bool> SubscribeToModAsync(string modId, string userId);
        
        /// <summary>
        /// Désabonne un utilisateur des notifications d'un mod
        /// </summary>
        Task<bool> UnsubscribeFromModAsync(string modId, string userId);
        
        /// <summary>
        /// Vérifie si un utilisateur est abonné à un mod
        /// </summary>
        Task<bool> IsUserSubscribedToModAsync(string modId, string userId);
        
        /// <summary>
        /// Récupère les notifications non lues d'un utilisateur
        /// </summary>
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
        
        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId);
        
        /// <summary>
        /// Marque toutes les notifications d'un utilisateur comme lues
        /// </summary>
        Task<bool> MarkAllNotificationsAsReadAsync(string userId);
    }
}

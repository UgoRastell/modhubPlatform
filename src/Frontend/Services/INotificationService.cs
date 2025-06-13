using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frontend.Models;
using Shared.Models;

namespace Frontend.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Récupère les notifications non lues de l'utilisateur
        /// </summary>
        Task<ApiResponse<IEnumerable<NotificationDto>>> GetUnreadNotificationsAsync();
        
        /// <summary>
        /// Récupère toutes les notifications de l'utilisateur
        /// </summary>
        Task<ApiResponse<IEnumerable<NotificationDto>>> GetAllNotificationsAsync();
        
        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        Task<ApiResponse<bool>> MarkNotificationAsReadAsync(string notificationId);
        
        /// <summary>
        /// Marque toutes les notifications comme lues
        /// </summary>
        Task<ApiResponse<bool>> MarkAllNotificationsAsReadAsync();
        
        /// <summary>
        /// S'abonne aux notifications d'un mod
        /// </summary>
        Task<ApiResponse<bool>> SubscribeToModAsync(string modId);
        
        /// <summary>
        /// Se désabonne des notifications d'un mod
        /// </summary>
        Task<ApiResponse<bool>> UnsubscribeFromModAsync(string modId);
        
        /// <summary>
        /// Vérifie si l'utilisateur est abonné aux notifications d'un mod
        /// </summary>
        Task<ApiResponse<bool>> IsSubscribedToModAsync(string modId);
        
        /// <summary>
        /// Affiche une notification de succès dans l'UI
        /// </summary>
        void ShowSuccess(string message);
        
        /// <summary>
        /// Affiche une notification d'erreur dans l'UI
        /// </summary>
        void ShowError(string message);
        
        /// <summary>
        /// Affiche une notification d'information dans l'UI
        /// </summary>
        void ShowInfo(string message);
        
        /// <summary>
        /// Affiche une notification d'avertissement dans l'UI
        /// </summary>
        void ShowWarning(string message);
    }
}

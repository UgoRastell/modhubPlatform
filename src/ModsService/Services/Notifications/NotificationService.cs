using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ModsService.Models;
using ModsService.Models.Notifications;
using ModsService.Repositories;
using ModsService.Repositories.Notifications;
using System.Text.Json;

namespace ModsService.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IModRepository _modRepository;
        private readonly ILogger<NotificationService> _logger;
        
        public NotificationService(
            INotificationRepository notificationRepository,
            IModRepository modRepository,
            ILogger<NotificationService> logger)
        {
            _notificationRepository = notificationRepository;
            _modRepository = modRepository;
            _logger = logger;
        }
        
        public async Task<bool> NotifyNewVersionAsync(string modId, ModVersion newVersion)
        {
            try
            {
                // Récupérer le mod pour extraire les données nécessaires
                var mod = await _modRepository.GetModByIdAsync(modId);
                
                if (mod == null)
                {
                    _logger.LogWarning($"Impossible d'envoyer des notifications: mod {modId} introuvable");
                    return false;
                }
                
                // Récupérer les utilisateurs abonnés à ce mod
                var subscribedUsers = await _modRepository.GetModSubscribersAsync(modId);
                
                if (subscribedUsers == null || !subscribedUsers.Any())
                {
                    _logger.LogInformation($"Aucun utilisateur abonné au mod {modId}");
                    return true; // Pas d'utilisateurs à notifier, considéré comme un succès
                }
                
                // Créer la notification
                var notification = new Notification
                {
                    Type = NotificationType.NewVersion,
                    Title = $"Nouvelle version de {mod.Name}",
                    Content = $"La version {newVersion.VersionNumber} de {mod.Name} est maintenant disponible.",
                    Url = $"/mods/{modId}",
                    RelatedIds = new Dictionary<string, string>
                    {
                        { "modId", modId },
                        { "versionId", newVersion.Id },
                        { "versionNumber", newVersion.VersionNumber }
                    },
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        mod.Name,
                        mod.CreatorId,
                        mod.CreatorName,
                        VersionName = newVersion.Name,
                        newVersion.VersionNumber,
                        ChangelogSummary = TruncateChangelog(newVersion.Changelog, 100)
                    })
                };
                
                // Envoyer la notification à chaque utilisateur abonné
                foreach (var userId in subscribedUsers)
                {
                    notification.Id = null; // Réinitialiser l'ID pour créer une nouvelle notification pour chaque utilisateur
                    notification.UserId = userId;
                    await _notificationRepository.AddNotificationAsync(notification);
                }
                
                _logger.LogInformation($"Notifications envoyées à {subscribedUsers.Count()} utilisateurs pour la nouvelle version {newVersion.VersionNumber} du mod {mod.Name}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi des notifications pour la nouvelle version {newVersion.VersionNumber} du mod {modId}");
                return false;
            }
        }
        
        public async Task<bool> NotifyUserAsync(string userId, Notification notification)
        {
            try
            {
                notification.UserId = userId;
                await _notificationRepository.AddNotificationAsync(notification);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi d'une notification à l'utilisateur {userId}");
                return false;
            }
        }
        
        public async Task<bool> NotifyUsersAsync(IEnumerable<string> userIds, Notification notification)
        {
            try
            {
                foreach (var userId in userIds)
                {
                    notification.Id = null; // Réinitialiser l'ID pour créer une nouvelle notification pour chaque utilisateur
                    notification.UserId = userId;
                    await _notificationRepository.AddNotificationAsync(notification);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de notifications à plusieurs utilisateurs");
                return false;
            }
        }
        
        public async Task<bool> SubscribeToModAsync(string modId, string userId)
        {
            return await _modRepository.AddModSubscriberAsync(modId, userId);
        }
        
        public async Task<bool> UnsubscribeFromModAsync(string modId, string userId)
        {
            return await _modRepository.RemoveModSubscriberAsync(modId, userId);
        }
        
        public async Task<bool> IsUserSubscribedToModAsync(string modId, string userId)
        {
            return await _modRepository.IsUserSubscribedToModAsync(modId, userId);
        }
        
        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId, unreadOnly);
        }
        
        public async Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId)
        {
            return await _notificationRepository.MarkNotificationAsReadAsync(notificationId, userId);
        }
        
        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            return await _notificationRepository.MarkAllNotificationsAsReadAsync(userId);
        }
        
        /// <summary>
        /// Tronque le changelog pour l'affichage dans les notifications
        /// </summary>
        private string TruncateChangelog(string changelog, int maxLength)
        {
            if (string.IsNullOrEmpty(changelog))
                return string.Empty;
                
            if (changelog.Length <= maxLength)
                return changelog;
                
            return changelog.Substring(0, maxLength) + "...";
        }
    }
}

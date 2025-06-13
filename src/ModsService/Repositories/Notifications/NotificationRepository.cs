using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ModsService.Models.Notifications;
using ModsService.Settings;

namespace ModsService.Repositories.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<Notification> _notifications;
        
        public NotificationRepository(IOptions<MongoDbSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _notifications = database.GetCollection<Notification>("Notifications");
            
            // Création d'index pour une recherche efficace
            CreateIndexes();
        }
        
        private void CreateIndexes()
        {
            // Index sur UserId pour rechercher rapidement les notifications d'un utilisateur
            _notifications.Indexes.CreateOne(
                new CreateIndexModel<Notification>(
                    Builders<Notification>.IndexKeys.Ascending(n => n.UserId)));
            
            // Index composé sur UserId et IsRead pour filtrer les notifications non lues d'un utilisateur
            _notifications.Indexes.CreateOne(
                new CreateIndexModel<Notification>(
                    Builders<Notification>.IndexKeys
                        .Ascending(n => n.UserId)
                        .Ascending(n => n.IsRead)));
            
            // Index sur CreatedAt pour le tri chronologique
            _notifications.Indexes.CreateOne(
                new CreateIndexModel<Notification>(
                    Builders<Notification>.IndexKeys.Descending(n => n.CreatedAt)));
                    
            // Index sur Type pour filtrer par type de notification
            _notifications.Indexes.CreateOne(
                new CreateIndexModel<Notification>(
                    Builders<Notification>.IndexKeys.Ascending(n => n.Type)));
        }
        
        public async Task<Notification> AddNotificationAsync(Notification notification)
        {
            await _notifications.InsertOneAsync(notification);
            return notification;
        }
        
        public async Task<Notification> GetNotificationByIdAsync(string id)
        {
            return await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync();
        }
        
        public async Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(string userId, bool unreadOnly = false)
        {
            var filterBuilder = Builders<Notification>.Filter;
            var filter = filterBuilder.Eq(n => n.UserId, userId);
            
            if (unreadOnly)
            {
                filter = filter & filterBuilder.Eq(n => n.IsRead, false);
            }
            
            return await _notifications.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<bool> MarkNotificationAsReadAsync(string notificationId, string userId)
        {
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.Id, notificationId),
                Builders<Notification>.Filter.Eq(n => n.UserId, userId));
            
            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow);
            
            var result = await _notifications.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        
        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.UserId, userId),
                Builders<Notification>.Filter.Eq(n => n.IsRead, false));
            
            var update = Builders<Notification>.Update
                .Set(n => n.IsRead, true)
                .Set(n => n.ReadAt, DateTime.UtcNow);
            
            var result = await _notifications.UpdateManyAsync(filter, update);
            return result.ModifiedCount > 0;
        }
        
        public async Task<bool> DeleteNotificationAsync(string id, string userId)
        {
            var filter = Builders<Notification>.Filter.And(
                Builders<Notification>.Filter.Eq(n => n.Id, id),
                Builders<Notification>.Filter.Eq(n => n.UserId, userId));
            
            var result = await _notifications.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        
        public async Task<IEnumerable<Notification>> GetNotificationsByFilterAsync(string userId, NotificationType? type = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var filterBuilder = Builders<Notification>.Filter;
            var filter = filterBuilder.Eq(n => n.UserId, userId);
            
            if (type.HasValue)
            {
                filter = filter & filterBuilder.Eq(n => n.Type, type.Value);
            }
            
            if (startDate.HasValue)
            {
                filter = filter & filterBuilder.Gte(n => n.CreatedAt, startDate.Value);
            }
            
            if (endDate.HasValue)
            {
                filter = filter & filterBuilder.Lte(n => n.CreatedAt, endDate.Value);
            }
            
            return await _notifications.Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}

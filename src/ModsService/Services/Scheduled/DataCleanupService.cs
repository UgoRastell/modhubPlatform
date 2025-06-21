using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModsService.Repositories;
using ModsService.Settings;

namespace ModsService.Services.Scheduled
{
    /// <summary>
    /// Service de nettoyage planifié des anciennes données d'historique
    /// </summary>
    public class DataCleanupService : BackgroundService
    {
        private readonly ILogger<DataCleanupService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DataRetentionSettings _settings;
        private Timer _timer;

        public DataCleanupService(
            ILogger<DataCleanupService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<DataRetentionSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de nettoyage des données démarré");

            // Exécuter immédiatement au démarrage
            _ = CleanupDataAsync();

            // Planifier les exécutions suivantes
            _timer = new Timer(async _ => await CleanupDataAsync(), null, 
                TimeSpan.FromHours(_settings.CleanupIntervalHours), 
                TimeSpan.FromHours(_settings.CleanupIntervalHours));

            return Task.CompletedTask;
        }

        private async Task CleanupDataAsync()
        {
            _logger.LogInformation("Démarrage du nettoyage planifié des données anciennes");

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var downloadHistoryRepository = scope.ServiceProvider.GetRequiredService<IDownloadHistoryRepository>();
                
                // Calculer la date de rétention pour l'historique détaillé
                var detailedHistoryCutoff = DateTime.UtcNow.AddDays(-_settings.DetailedHistoryRetentionDays);
                
                // Nettoyer les données d'historique détaillées
                var detailedDeleted = await downloadHistoryRepository.DeleteDownloadHistoryBeforeDateAsync(detailedHistoryCutoff);
                _logger.LogInformation($"Nettoyage des données détaillées: {detailedDeleted} enregistrements supprimés (avant {detailedHistoryCutoff})");

                // Agréger les données quotidiennes avant suppression
                if (_settings.AggregateBeforeDelete)
                {
                    var aggregationCutoff = DateTime.UtcNow.AddDays(-_settings.AggregationThresholdDays);
                    var aggregatedCount = await downloadHistoryRepository.AggregateDownloadsBeforeDateAsync(aggregationCutoff);
                    _logger.LogInformation($"Agrégation des données: {aggregatedCount} téléchargements agrégés (avant {aggregationCutoff})");
                }

                // Nettoyer les anciennes notifications
                if (scope.ServiceProvider.GetService<INotificationRepository>() is INotificationRepository notificationRepository)
                {
                    var notificationsCutoff = DateTime.UtcNow.AddDays(-_settings.NotificationsRetentionDays);
                    var notificationsDeleted = await notificationRepository.DeleteOldReadNotificationsAsync(notificationsCutoff);
                    _logger.LogInformation($"Nettoyage des notifications: {notificationsDeleted} notifications supprimées (avant {notificationsCutoff})");
                }
                
                _logger.LogInformation("Nettoyage planifié des données terminé avec succès");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage planifié des données");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de nettoyage des données arrêté");
            
            _timer?.Change(Timeout.Infinite, 0);
            
            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}

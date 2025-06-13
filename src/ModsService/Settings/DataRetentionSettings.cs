namespace ModsService.Settings
{
    /// <summary>
    /// Paramètres de rétention des données pour le service de nettoyage
    /// </summary>
    public class DataRetentionSettings
    {
        /// <summary>
        /// Intervalle en heures entre chaque exécution du nettoyage
        /// </summary>
        public int CleanupIntervalHours { get; set; } = 24;
        
        /// <summary>
        /// Nombre de jours de conservation des données d'historique détaillées
        /// </summary>
        public int DetailedHistoryRetentionDays { get; set; } = 90;
        
        /// <summary>
        /// Nombre de jours avant agrégation des données quotidiennes
        /// </summary>
        public int AggregationThresholdDays { get; set; } = 30;
        
        /// <summary>
        /// Indique si les données doivent être agrégées avant suppression
        /// </summary>
        public bool AggregateBeforeDelete { get; set; } = true;
        
        /// <summary>
        /// Nombre de jours de conservation des notifications lues
        /// </summary>
        public int NotificationsRetentionDays { get; set; } = 60;
    }
}

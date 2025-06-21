using System;
using System.Collections.Generic;

namespace ModsService.Models
{
    /// <summary>
    /// Représente un enregistrement de téléchargement d'un mod
    /// </summary>
    public class DownloadHistory
    {
        /// <summary>
        /// Identifiant unique de l'enregistrement
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Identifiant du mod téléchargé
        /// </summary>
        public string ModId { get; set; }
        
        /// <summary>
        /// Nom du mod (pour faciliter les requêtes sans jointure)
        /// </summary>
        public string ModName { get; set; }
        
        /// <summary>
        /// Version du mod téléchargée
        /// </summary>
        public string VersionNumber { get; set; }
        
        /// <summary>
        /// ID de la version du mod téléchargée
        /// </summary>
        public string VersionId { get; set; }
        
        /// <summary>
        /// Identifiant de l'utilisateur ayant téléchargé le mod (null si anonyme)
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Adresse IP de l'utilisateur (anonymisée pour RGPD)
        /// </summary>
        public string AnonymizedIp { get; set; }
        
        /// <summary>
        /// Date et heure du téléchargement
        /// </summary>
        public DateTime DownloadedAt { get; set; }
        
        /// <summary>
        /// Type d'appareil utilisé pour le téléchargement
        /// </summary>
        public string DeviceType { get; set; }
        
        /// <summary>
        /// Type de navigateur utilisé
        /// </summary>
        public string UserAgent { get; set; }
        
        /// <summary>
        /// Pays d'origine du téléchargement
        /// </summary>
        public string Country { get; set; }
        
        /// <summary>
        /// Référent (d'où vient l'utilisateur)
        /// </summary>
        public string Referrer { get; set; }
        
        /// <summary>
        /// Taille du fichier téléchargé en octets
        /// </summary>
        public long FileSizeBytes { get; set; }
        
        /// <summary>
        /// Temps de téléchargement en millisecondes
        /// </summary>
        public int DownloadTimeMs { get; set; }
        
        /// <summary>
        /// Statut du téléchargement
        /// </summary>
        public DownloadStatus Status { get; set; }
        
        /// <summary>
        /// Indique s'il s'agit d'une reprise de téléchargement
        /// </summary>
        public bool IsResume { get; set; }
        
        /// <summary>
        /// Informations supplémentaires en format JSON
        /// </summary>
        public string AdditionalInfo { get; set; }
    }
    
    /// <summary>
    /// Statut d'un téléchargement
    /// </summary>
    public enum DownloadStatus
    {
        /// <summary>
        /// Téléchargement démarré
        /// </summary>
        Started,
        
        /// <summary>
        /// Téléchargement terminé avec succès
        /// </summary>
        Completed,
        
        /// <summary>
        /// Téléchargement annulé
        /// </summary>
        Canceled,
        
        /// <summary>
        /// Téléchargement échoué
        /// </summary>
        Failed,
        
        /// <summary>
        /// Téléchargement limité par quota
        /// </summary>
        QuotaLimited
    }
    
    /// <summary>
    /// Statistiques de téléchargement par période
    /// </summary>
    public class DownloadStatistics
    {
        /// <summary>
        /// Identifiant du mod concerné par les statistiques
        /// </summary>
        public string ModId { get; set; }
        
        /// <summary>
        /// Statistiques par version
        /// </summary>
        public Dictionary<string, VersionStatistics> StatsByVersion { get; set; } = new Dictionary<string, VersionStatistics>();
        
        /// <summary>
        /// Statistiques par jour (clé au format YYYY-MM-DD)
        /// </summary>
        public Dictionary<string, DailyStatistics> DailyStats { get; set; } = new Dictionary<string, DailyStatistics>();
        
        /// <summary>
        /// Nombre total de téléchargements
        /// </summary>
        public long TotalDownloads { get; set; }
        
        /// <summary>
        /// Nombre de téléchargements uniques (par utilisateur/IP)
        /// </summary>
        public long UniqueDownloads { get; set; }
        
        /// <summary>
        /// Répartition géographique des téléchargements (par pays)
        /// </summary>
        public Dictionary<string, long> GeographicDistribution { get; set; } = new Dictionary<string, long>();
        
        /// <summary>
        /// Répartition par type d'appareil
        /// </summary>
        public Dictionary<string, long> DeviceDistribution { get; set; } = new Dictionary<string, long>();
        
        /// <summary>
        /// Taille totale téléchargée en octets
        /// </summary>
        public long TotalSizeBytes { get; set; }
        
        /// <summary>
        /// Date de la dernière mise à jour des statistiques
        /// </summary>
        public DateTime LastUpdated { get; set; }
    }
    
    /// <summary>
    /// Statistiques par version
    /// </summary>
    public class VersionStatistics
    {
        /// <summary>
        /// Numéro de version
        /// </summary>
        public string VersionNumber { get; set; }
        
        /// <summary>
        /// Nombre de téléchargements
        /// </summary>
        public long DownloadCount { get; set; }
        
        /// <summary>
        /// Date du premier téléchargement
        /// </summary>
        public DateTime FirstDownload { get; set; }
        
        /// <summary>
        /// Date du dernier téléchargement
        /// </summary>
        public DateTime LastDownload { get; set; }
        
        /// <summary>
        /// Taille totale téléchargée en octets
        /// </summary>
        public long TotalSizeBytes { get; set; }
    }
    
    /// <summary>
    /// Statistiques journalières
    /// </summary>
    public class DailyStatistics
    {
        /// <summary>
        /// Date concernée
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// Nombre total de téléchargements
        /// </summary>
        public long DownloadCount { get; set; }
        
        /// <summary>
        /// Téléchargements par heure (0-23)
        /// </summary>
        public long[] DownloadsByHour { get; set; } = new long[24];
        
        /// <summary>
        /// Téléchargements par version
        /// </summary>
        public Dictionary<string, long> DownloadsByVersion { get; set; } = new Dictionary<string, long>();
    }
}

using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    public class DownloadStatsDto
    {
        // Propriétés existantes pour les statistiques générales
        public int DownloadsToday { get; set; }
        public int DownloadsThisWeek { get; set; }
        public int UsersAtQuotaLimit { get; set; }
        public int BlockedIPs { get; set; }
        
        // Propriétés pour les graphiques et analyses détaillées
        public Dictionary<DateTime, int> DailyDownloads { get; set; } = new Dictionary<DateTime, int>();
        public Dictionary<string, int> VersionDownloads { get; set; } = new Dictionary<string, int>();
        public int TotalDownloads { get; set; }
    }
}

using System;

namespace ModsService.Models
{
    /// <summary>
    /// Représente un quota de téléchargement pour un utilisateur
    /// </summary>
    public class DownloadQuota
    {
        /// <summary>
        /// Identifiant unique du quota
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Identifiant de l'utilisateur concerné par ce quota
        /// </summary>
        public string UserId { get; set; }
        
        /// <summary>
        /// Type de quota (Daily, Weekly, Monthly)
        /// </summary>
        public QuotaType Type { get; set; }
        
        /// <summary>
        /// Limite de téléchargement (nombre de mods ou taille en Mo)
        /// </summary>
        public int Limit { get; set; }
        
        /// <summary>
        /// Utilisation actuelle du quota
        /// </summary>
        public int CurrentUsage { get; set; }
        
        /// <summary>
        /// Dernière réinitialisation du quota
        /// </summary>
        public DateTime LastReset { get; set; }
        
        /// <summary>
        /// Prochaine réinitialisation du quota
        /// </summary>
        public DateTime NextReset { get; set; }
        
        /// <summary>
        /// Indique si le quota est bloquant (si true, l'utilisateur ne peut plus télécharger après dépassement)
        /// Si false, c'est juste un avertissement
        /// </summary>
        public bool IsBlocking { get; set; }
        
        /// <summary>
        /// Calcule le quota restant
        /// </summary>
        public int RemainingQuota => Math.Max(0, Limit - CurrentUsage);
        
        /// <summary>
        /// Vérifie si l'utilisateur a dépassé son quota
        /// </summary>
        public bool HasExceededQuota => CurrentUsage >= Limit;
    }
    
    /// <summary>
    /// Type de quota de téléchargement
    /// </summary>
    public enum QuotaType
    {
        /// <summary>
        /// Quota journalier
        /// </summary>
        Daily,
        
        /// <summary>
        /// Quota hebdomadaire
        /// </summary>
        Weekly,
        
        /// <summary>
        /// Quota mensuel
        /// </summary>
        Monthly
    }
}

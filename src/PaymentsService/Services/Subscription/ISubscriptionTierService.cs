using PaymentsService.Models.Subscription;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentsService.Services.Subscription
{
    /// <summary>
    /// Interface pour le service de gestion des niveaux d'abonnement
    /// </summary>
    public interface ISubscriptionTierService
    {
        /// <summary>
        /// Récupère tous les niveaux d'abonnement disponibles
        /// </summary>
        Task<List<SubscriptionTier>> GetAllTiersAsync();
        
        /// <summary>
        /// Récupère un niveau d'abonnement par son ID
        /// </summary>
        Task<SubscriptionTier> GetTierByIdAsync(string tierId);
        
        /// <summary>
        /// Récupère un niveau d'abonnement par son ID externe (Stripe, etc.)
        /// </summary>
        Task<SubscriptionTier> GetTierByExternalIdAsync(string externalId);
        
        /// <summary>
        /// Crée un nouveau niveau d'abonnement
        /// </summary>
        Task<SubscriptionTier> CreateTierAsync(SubscriptionTier tier);
        
        /// <summary>
        /// Met à jour un niveau d'abonnement existant
        /// </summary>
        Task<bool> UpdateTierAsync(string tierId, SubscriptionTier tier);
        
        /// <summary>
        /// Supprime un niveau d'abonnement
        /// </summary>
        Task<bool> DeleteTierAsync(string tierId);
        
        /// <summary>
        /// Active ou désactive un niveau d'abonnement
        /// </summary>
        Task<bool> ToggleTierActiveStatusAsync(string tierId, bool isActive);
        
        /// <summary>
        /// Ajoute un avantage à un niveau d'abonnement
        /// </summary>
        Task<bool> AddBenefitToTierAsync(string tierId, SubscriptionBenefit benefit);
        
        /// <summary>
        /// Supprime un avantage d'un niveau d'abonnement
        /// </summary>
        Task<bool> RemoveBenefitFromTierAsync(string tierId, string benefitName);
        
        /// <summary>
        /// Récupère l'historique des modifications de prix d'un niveau d'abonnement
        /// </summary>
        Task<List<SubscriptionTierHistory>> GetTierPriceHistoryAsync(string tierId);
        
        /// <summary>
        /// Met à jour le prix d'un niveau d'abonnement avec historisation
        /// </summary>
        Task<bool> UpdateTierPriceAsync(
            string tierId, 
            decimal? newMonthlyPrice, 
            decimal? newYearlyPrice, 
            string userId, 
            string changeNotes);
        
        /// <summary>
        /// Compare les avantages entre différents niveaux d'abonnement
        /// </summary>
        Task<Dictionary<string, List<TierComparisonItem>>> CompareTiersAsync(List<string> tierIds);
        
        /// <summary>
        /// Récupère les statistiques d'utilisation des différents niveaux d'abonnement
        /// </summary>
        Task<List<TierStatistics>> GetTierStatisticsAsync();
        
        /// <summary>
        /// Vérifie si un utilisateur a accès à une fonctionnalité spécifique selon son niveau d'abonnement
        /// </summary>
        Task<bool> CheckUserAccessToFeatureAsync(string userId, string featureName);
        
        /// <summary>
        /// Récupère la liste des fonctionnalités auxquelles un utilisateur a accès selon son niveau d'abonnement
        /// </summary>
        Task<List<string>> GetUserAccessibleFeaturesAsync(string userId);
    }
    
    /// <summary>
    /// Élément de comparaison entre différents niveaux d'abonnement
    /// </summary>
    public class TierComparisonItem
    {
        /// <summary>
        /// Nom de la fonctionnalité comparée
        /// </summary>
        public string FeatureName { get; set; } = string.Empty;
        
        /// <summary>
        /// Catégorie de la fonctionnalité
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Valeur pour chaque niveau d'abonnement comparé
        /// </summary>
        public Dictionary<string, string> TierValues { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Si cette fonctionnalité est considérée comme principale/importante
        /// </summary>
        public bool IsKeyFeature { get; set; } = false;
    }
    
    /// <summary>
    /// Statistiques d'utilisation d'un niveau d'abonnement
    /// </summary>
    public class TierStatistics
    {
        /// <summary>
        /// ID du niveau d'abonnement
        /// </summary>
        public string TierId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du niveau d'abonnement
        /// </summary>
        public string TierName { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre total d'abonnés actifs
        /// </summary>
        public int ActiveSubscribers { get; set; } = 0;
        
        /// <summary>
        /// Répartition des abonnés mensuels vs annuels
        /// </summary>
        public Dictionary<string, int> BillingCycleDistribution { get; set; } = new Dictionary<string, int>();
        
        /// <summary>
        /// Revenu mensuel récurrent (MRR)
        /// </summary>
        public decimal MonthlyRecurringRevenue { get; set; } = 0;
        
        /// <summary>
        /// Taux de conversion des essais en abonnements payants
        /// </summary>
        public float TrialConversionRate { get; set; } = 0;
        
        /// <summary>
        /// Taux de rétention des abonnés
        /// </summary>
        public float RetentionRate { get; set; } = 0;
        
        /// <summary>
        /// Taux de désabonnement
        /// </summary>
        public float ChurnRate { get; set; } = 0;
        
        /// <summary>
        /// Durée de vie moyenne d'un abonnement (en mois)
        /// </summary>
        public float AverageSubscriptionLifetime { get; set; } = 0;
        
        /// <summary>
        /// Valeur à vie moyenne d'un client (LTV)
        /// </summary>
        public decimal AverageCustomerLifetimeValue { get; set; } = 0;
    }
}

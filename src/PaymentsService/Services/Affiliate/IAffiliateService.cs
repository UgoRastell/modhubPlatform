using PaymentsService.Models.Affiliate;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PaymentsService.Services.Affiliate
{
    /// <summary>
    /// Interface pour le service de gestion du programme d'affiliation
    /// </summary>
    public interface IAffiliateService
    {
        /// <summary>
        /// Récupère tous les programmes d'affiliation
        /// </summary>
        Task<List<AffiliateProgram>> GetAllProgramsAsync();
        
        /// <summary>
        /// Récupère un programme d'affiliation par son ID
        /// </summary>
        Task<AffiliateProgram> GetProgramByIdAsync(string programId);
        
        /// <summary>
        /// Crée un programme d'affiliation
        /// </summary>
        Task<AffiliateProgram> CreateProgramAsync(AffiliateProgram program);
        
        /// <summary>
        /// Met à jour un programme d'affiliation
        /// </summary>
        Task<bool> UpdateProgramAsync(string programId, AffiliateProgram program);
        
        /// <summary>
        /// Supprime un programme d'affiliation
        /// </summary>
        Task<bool> DeleteProgramAsync(string programId);
        
        /// <summary>
        /// Génère un lien d'affiliation pour un utilisateur
        /// </summary>
        Task<AffiliateLink> GenerateAffiliateLinkAsync(string userId, string targetType, string targetId);
        
        /// <summary>
        /// Enregistre un clic sur un lien d'affiliation
        /// </summary>
        Task<AffiliateClick> TrackAffiliateLinkClickAsync(string linkId, string ipAddress, string userAgent);
        
        /// <summary>
        /// Enregistre une conversion (vente via un lien d'affiliation)
        /// </summary>
        Task<AffiliateConversion> TrackConversionAsync(string clickId, decimal amount, string orderId);
        
        /// <summary>
        /// Récupère les commissions d'un affilié
        /// </summary>
        Task<List<AffiliateCommission>> GetAffiliateCommissionsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Calcule les commissions à payer pour une période donnée
        /// </summary>
        Task<decimal> CalculatePendingCommissionsAsync(string userId);
        
        /// <summary>
        /// Demande un paiement de commission
        /// </summary>
        Task<AffiliatePayoutRequest> RequestPayoutAsync(string userId, decimal amount);
        
        /// <summary>
        /// Récupère les statistiques d'affilié pour un utilisateur
        /// </summary>
        Task<AffiliateStatistics> GetAffiliateStatisticsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Met à jour le niveau d'un affilié en fonction de ses performances
        /// </summary>
        Task<AffiliateLevel> UpdateAffiliateLevelAsync(string userId);
    }
    
    /// <summary>
    /// Lien d'affiliation généré pour un utilisateur
    /// </summary>
    public class AffiliateLink
    {
        /// <summary>
        /// Identifiant unique du lien
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur affilié
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Code unique pour le lien (ex: "john-s-promo")
        /// </summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de la cible (mod, abonnement, etc.)
        /// </summary>
        public string TargetType { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la cible (mod, abonnement, etc.)
        /// </summary>
        public string TargetId { get; set; } = string.Empty;
        
        /// <summary>
        /// URL complète du lien d'affiliation
        /// </summary>
        public string FullUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de création du lien
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Nombre total de clics sur ce lien
        /// </summary>
        public int TotalClicks { get; set; } = 0;
        
        /// <summary>
        /// Nombre total de conversions via ce lien
        /// </summary>
        public int TotalConversions { get; set; } = 0;
    }
    
    /// <summary>
    /// Enregistrement d'un clic sur un lien d'affiliation
    /// </summary>
    public class AffiliateClick
    {
        /// <summary>
        /// Identifiant unique du clic
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du lien d'affiliation
        /// </summary>
        public string LinkId { get; set; } = string.Empty;
        
        /// <summary>
        /// Adresse IP de l'utilisateur (anonymisée)
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// Agent utilisateur (navigateur)
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;
        
        /// <summary>
        /// Date du clic
        /// </summary>
        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Si une conversion a eu lieu suite à ce clic
        /// </summary>
        public bool Converted { get; set; } = false;
        
        /// <summary>
        /// Date d'expiration du cookie d'affiliation
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
    
    /// <summary>
    /// Enregistrement d'une conversion via un lien d'affiliation
    /// </summary>
    public class AffiliateConversion
    {
        /// <summary>
        /// Identifiant unique de la conversion
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du clic d'origine
        /// </summary>
        public string ClickId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du lien d'affiliation
        /// </summary>
        public string LinkId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur affilié
        /// </summary>
        public string AffiliateUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant de la vente
        /// </summary>
        public decimal SaleAmount { get; set; }
        
        /// <summary>
        /// Montant de la commission
        /// </summary>
        public decimal CommissionAmount { get; set; }
        
        /// <summary>
        /// ID de la commande liée
        /// </summary>
        public string OrderId { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de la conversion
        /// </summary>
        public DateTime ConvertedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statut de la commission (En attente, Payée, Annulée)
        /// </summary>
        public string Status { get; set; } = "Pending";
    }
    
    /// <summary>
    /// Commission pour un affilié
    /// </summary>
    public class AffiliateCommission
    {
        /// <summary>
        /// Identifiant unique de la commission
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur affilié
        /// </summary>
        public string AffiliateUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de la conversion
        /// </summary>
        public string ConversionId { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant de la commission
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Description de la commission
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de création de la commission
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statut de la commission (En attente, Payée, Annulée)
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// Date de paiement (si payée)
        /// </summary>
        public DateTime? PaidAt { get; set; }
        
        /// <summary>
        /// ID de la demande de paiement associée
        /// </summary>
        public string? PayoutRequestId { get; set; }
    }
    
    /// <summary>
    /// Demande de paiement de commissions
    /// </summary>
    public class AffiliatePayoutRequest
    {
        /// <summary>
        /// Identifiant unique de la demande
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur affilié
        /// </summary>
        public string AffiliateUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant demandé
        /// </summary>
        public decimal Amount { get; set; }
        
        /// <summary>
        /// Liste des ID de commissions incluses
        /// </summary>
        public List<string> CommissionIds { get; set; } = new List<string>();
        
        /// <summary>
        /// Méthode de paiement (PayPal, virement bancaire, etc.)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;
        
        /// <summary>
        /// Informations de paiement (email PayPal, etc.)
        /// </summary>
        public string PaymentDetails { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de la demande
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statut de la demande (En attente, Traitée, Rejetée)
        /// </summary>
        public string Status { get; set; } = "Pending";
        
        /// <summary>
        /// Date de traitement
        /// </summary>
        public DateTime? ProcessedAt { get; set; }
        
        /// <summary>
        /// Notes administratives
        /// </summary>
        public string? AdminNotes { get; set; }
    }
    
    /// <summary>
    /// Statistiques pour un affilié
    /// </summary>
    public class AffiliateStatistics
    {
        /// <summary>
        /// ID de l'utilisateur affilié
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre total de clics
        /// </summary>
        public int TotalClicks { get; set; }
        
        /// <summary>
        /// Nombre total de conversions
        /// </summary>
        public int TotalConversions { get; set; }
        
        /// <summary>
        /// Taux de conversion (%)
        /// </summary>
        public decimal ConversionRate { get; set; }
        
        /// <summary>
        /// Total des commissions gagnées
        /// </summary>
        public decimal TotalCommissionEarned { get; set; }
        
        /// <summary>
        /// Commissions payées
        /// </summary>
        public decimal CommissionPaid { get; set; }
        
        /// <summary>
        /// Commissions en attente
        /// </summary>
        public decimal CommissionPending { get; set; }
        
        /// <summary>
        /// Niveau actuel d'affiliation
        /// </summary>
        public AffiliateLevel CurrentLevel { get; set; } = null!;
        
        /// <summary>
        /// Progression vers le prochain niveau (%)
        /// </summary>
        public int NextLevelProgress { get; set; }
    }
}

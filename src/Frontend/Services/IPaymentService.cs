using Frontend.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services
{
    /// <summary>
    /// Service de gestion des paiements via Stripe
    /// </summary>
    public interface IPaymentService
    {
        /// <summary>
        /// Crée une intention de paiement pour un produit ou service
        /// </summary>
        /// <param name="request">Les détails de la demande de paiement</param>
        /// <returns>La réponse contenant le secret client pour finaliser le paiement</returns>
        Task<PaymentIntentResponse> CreatePaymentIntentAsync(PaymentIntentRequest request);
        
        /// <summary>
        /// Récupère l'historique des paiements d'un utilisateur
        /// </summary>
        /// <param name="userId">Identifiant de l'utilisateur</param>
        /// <returns>Liste des paiements de l'utilisateur</returns>
        Task<List<PaymentHistory>> GetPaymentHistoryAsync(string userId);
        
        /// <summary>
        /// Récupère les statistiques générales des paiements pour le tableau de bord d'administration
        /// </summary>
        /// <returns>Les statistiques de paiement</returns>
        Task<PaymentStatistics> GetPaymentStatisticsAsync();
        
        /// <summary>
        /// Récupère les données pour les graphiques de revenus par période
        /// </summary>
        /// <param name="period">La période (day, week, month, year)</param>
        /// <param name="count">Le nombre de périodes à récupérer</param>
        /// <returns>Les données formatées pour les graphiques</returns>
        Task<PaymentChartData> GetRevenueChartDataAsync(string period = "day", int count = 30);
        
        /// <summary>
        /// Récupère les transactions nécessitant une intervention manuelle
        /// </summary>
        /// <param name="minPriority">Priorité minimale (1-5)</param>
        /// <param name="maxResults">Nombre maximum de résultats</param>
        /// <returns>Liste des transactions à examiner</returns>
        Task<List<ReviewTransaction>> GetTransactionsRequiringReviewAsync(int minPriority = 1, int maxResults = 50);
        
        /// <summary>
        /// Effectue un remboursement pour une transaction
        /// </summary>
        /// <param name="paymentId">Identifiant du paiement à rembourser</param>
        /// <param name="amount">Montant à rembourser (en centimes). Si null, rembourse le montant total</param>
        /// <param name="reason">Raison du remboursement</param>
        /// <returns>True si le remboursement a réussi</returns>
        Task<bool> RefundPaymentAsync(string paymentId, long? amount = null, string reason = "");
    }

    /// <summary>
    /// Représentation simplifiée d'un paiement pour l'UI
    /// </summary>
    public class PaymentIntent
    {
        public string Id { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

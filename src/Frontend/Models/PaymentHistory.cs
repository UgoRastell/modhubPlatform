using System;
using System.Collections.Generic;

namespace Frontend.Models
{
    /// <summary>
    /// Représente l'historique des paiements d'un utilisateur
    /// </summary>
    public class PaymentHistory
    {
        /// <summary>
        /// Identifiant du paiement
        /// </summary>
        public string PaymentId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du produit acheté
        /// </summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// Description du produit ou service acheté
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Montant du paiement en centimes
        /// </summary>
        public long Amount { get; set; }

        /// <summary>
        /// Devise du paiement (ex: EUR, USD)
        /// </summary>
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Statut du paiement (succeeded, pending, failed, refunded, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Date du paiement
        /// </summary>
        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Indique si ce paiement fait partie d'un abonnement récurrent
        /// </summary>
        public bool IsRecurring { get; set; }

        /// <summary>
        /// Informations supplémentaires sur le paiement au format JSON
        /// </summary>
        public string AdditionalData { get; set; } = string.Empty;
    }

    /// <summary>
    /// Liste des paiements d'un utilisateur
    /// </summary>
    public class PaymentHistoryList
    {
        /// <summary>
        /// Liste des paiements
        /// </summary>
        public List<PaymentHistory> Payments { get; set; } = new List<PaymentHistory>();
    }
}

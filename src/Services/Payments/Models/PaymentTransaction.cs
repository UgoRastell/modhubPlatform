using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PaymentsService.Models
{
    /// <summary>
    /// Représente une transaction de paiement complète avec son historique d'événements
    /// </summary>
    public class PaymentTransaction
    {
        /// <summary>
        /// Identifiant unique de la transaction
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Identifiant externe (par exemple, l'ID Stripe)
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de la transaction (payment_intent, subscription, refund, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant du produit ou service
        /// </summary>
        public string ProductId { get; set; } = string.Empty;
        
        /// <summary>
        /// Description de la transaction
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant de la transaction en centimes
        /// </summary>
        public long Amount { get; set; }
        
        /// <summary>
        /// Devise de la transaction
        /// </summary>
        public string Currency { get; set; } = string.Empty;
        
        /// <summary>
        /// Statut actuel de la transaction
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Indique si la transaction est récurrente (abonnement)
        /// </summary>
        public bool IsRecurring { get; set; }
        
        /// <summary>
        /// Fréquence de récurrence si applicable
        /// </summary>
        public string RecurringFrequency { get; set; } = string.Empty;
        
        /// <summary>
        /// Date et heure de création de la transaction
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date et heure de dernière mise à jour
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Erreur éventuelle liée à la transaction
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// Indique si la transaction nécessite une intervention manuelle
        /// </summary>
        public bool RequiresReview { get; set; }
        
        /// <summary>
        /// Raison de l'intervention manuelle si applicable
        /// </summary>
        public string ReviewReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Priorité de l'intervention (1-5)
        /// </summary>
        public int ReviewPriority { get; set; }
        
        /// <summary>
        /// Métadonnées additionnelles de la transaction
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        
        /// <summary>
        /// Historique des événements liés à la transaction
        /// </summary>
        public List<TransactionEvent> Events { get; set; } = new List<TransactionEvent>();
    }
    
    /// <summary>
    /// Représente un événement dans l'historique d'une transaction
    /// </summary>
    public class TransactionEvent
    {
        /// <summary>
        /// Identifiant unique de l'événement
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type d'événement (created, updated, payment_failed, etc.)
        /// </summary>
        public string EventType { get; set; } = string.Empty;
        
        /// <summary>
        /// Description de l'événement
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Date et heure de l'événement
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Métadonnées additionnelles de l'événement
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}

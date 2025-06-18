using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Frontend.Models
{
    /// <summary>
    /// Statistiques générales des paiements
    /// </summary>
    public class PaymentStatistics
    {
        /// <summary>
        /// Montant total des revenus (en centimes)
        /// </summary>
        [JsonPropertyName("totalRevenue")]
        public long TotalRevenue { get; set; }
        
        /// <summary>
        /// Nombre total de transactions
        /// </summary>
        [JsonPropertyName("totalTransactions")]
        public int TotalTransactions { get; set; }
        
        /// <summary>
        /// Montant moyen par transaction (en centimes)
        /// </summary>
        [JsonPropertyName("averageTransactionAmount")]
        public long AverageTransactionAmount { get; set; }
        
        /// <summary>
        /// Nombre de transactions réussies
        /// </summary>
        [JsonPropertyName("successfulTransactions")]
        public int SuccessfulTransactions { get; set; }
        
        /// <summary>
        /// Nombre de transactions échouées
        /// </summary>
        [JsonPropertyName("failedTransactions")]
        public int FailedTransactions { get; set; }
        
        /// <summary>
        /// Nombre de remboursements
        /// </summary>
        [JsonPropertyName("refunds")]
        public int Refunds { get; set; }
        
        /// <summary>
        /// Montant total des remboursements (en centimes)
        /// </summary>
        [JsonPropertyName("totalRefundAmount")]
        public long TotalRefundAmount { get; set; }
        
        /// <summary>
        /// Nombre d'abonnements actifs
        /// </summary>
        [JsonPropertyName("activeSubscriptions")]
        public int ActiveSubscriptions { get; set; }
        
        /// <summary>
        /// Revenu mensuel récurrent (MRR) en centimes
        /// </summary>
        [JsonPropertyName("monthlyRecurringRevenue")]
        public long MonthlyRecurringRevenue { get; set; }
        
        /// <summary>
        /// Nombre de transactions nécessitant une intervention
        /// </summary>
        [JsonPropertyName("transactionsRequiringReview")]
        public int TransactionsRequiringReview { get; set; }
        
        /// <summary>
        /// Nombre de transactions par statut
        /// </summary>
        [JsonPropertyName("transactionsByStatus")]
        public Dictionary<string, int> TransactionsByStatus { get; set; } = new Dictionary<string, int>();
        
        /// <summary>
        /// Revenus par période (jour, semaine, mois)
        /// </summary>
        [JsonPropertyName("revenueByPeriod")]
        public Dictionary<string, long> RevenueByPeriod { get; set; } = new Dictionary<string, long>();
        
        /// <summary>
        /// Transactions par méthode de paiement
        /// </summary>
        [JsonPropertyName("transactionsByPaymentMethod")]
        public Dictionary<string, int> TransactionsByPaymentMethod { get; set; } = new Dictionary<string, int>();
    }
    
    /// <summary>
    /// Données pour les graphiques de paiement
    /// </summary>
    public class PaymentChartData
    {
        /// <summary>
        /// Titre du graphique
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Labels pour l'axe X
        /// </summary>
        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = new List<string>();
        
        /// <summary>
        /// Séries de données
        /// </summary>
        [JsonPropertyName("datasets")]
        public List<ChartDataset> Datasets { get; set; } = new List<ChartDataset>();
    }
    
    /// <summary>
    /// Série de données pour les graphiques
    /// </summary>
    public class ChartDataset
    {
        /// <summary>
        /// Nom de la série
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Valeurs des données
        /// </summary>
        [JsonPropertyName("data")]
        public List<long> Data { get; set; } = new List<long>();
        
        /// <summary>
        /// Couleur pour cette série
        /// </summary>
        [JsonPropertyName("color")]
        public string Color { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de graphique (line, bar, pie)
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "line";
    }
    
    /// <summary>
    /// Transaction nécessitant une intervention manuelle
    /// </summary>
    public class ReviewTransaction
    {
        /// <summary>
        /// Identifiant de la transaction
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant externe (Stripe)
        /// </summary>
        [JsonPropertyName("externalId")]
        public string ExternalId { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant de l'utilisateur
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Montant de la transaction en centimes
        /// </summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }
        
        /// <summary>
        /// Devise de la transaction
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
        
        /// <summary>
        /// Statut de la transaction
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de création
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// Raison de l'intervention nécessaire
        /// </summary>
        [JsonPropertyName("reviewReason")]
        public string ReviewReason { get; set; } = string.Empty;
        
        /// <summary>
        /// Priorité de l'intervention (1-5)
        /// </summary>
        [JsonPropertyName("reviewPriority")]
        public int ReviewPriority { get; set; }
    }
}

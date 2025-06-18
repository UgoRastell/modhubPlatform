using System;
using System.Text.Json.Serialization;

namespace PaymentsService.Models.DTOs
{
    /// <summary>
    /// Réponse contenant les informations d'un abonnement créé
    /// </summary>
    public class SubscriptionResponse
    {
        /// <summary>
        /// Identifiant de l'abonnement Stripe
        /// </summary>
        [JsonPropertyName("subscriptionId")]
        public string SubscriptionId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du client Stripe
        /// </summary>
        [JsonPropertyName("customerId")]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant du produit ou plan souscrit
        /// </summary>
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// Status de l'abonnement (active, past_due, canceled, etc.)
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Date de début de l'abonnement
        /// </summary>
        [JsonPropertyName("currentPeriodStart")]
        public DateTime CurrentPeriodStart { get; set; }

        /// <summary>
        /// Date de fin de la période actuelle
        /// </summary>
        [JsonPropertyName("currentPeriodEnd")]
        public DateTime CurrentPeriodEnd { get; set; }

        /// <summary>
        /// Montant périodique de l'abonnement en centimes
        /// </summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        /// <summary>
        /// Devise de l'abonnement (EUR, USD, etc.)
        /// </summary>
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        /// <summary>
        /// Indique si l'abonnement se renouvelle automatiquement
        /// </summary>
        [JsonPropertyName("cancelAtPeriodEnd")]
        public bool CancelAtPeriodEnd { get; set; }

        /// <summary>
        /// Fréquence de facturation (monthly, yearly, etc.)
        /// </summary>
        [JsonPropertyName("interval")]
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Secret client pour configuration du paiement côté client
        /// </summary>
        [JsonPropertyName("clientSecret")]
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// Clé publique Stripe pour le frontend
        /// </summary>
        [JsonPropertyName("publishableKey")]
        public string PublishableKey { get; set; } = string.Empty;
    }
}

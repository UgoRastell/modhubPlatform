using System;
using System.Text.Json.Serialization;

namespace PaymentsService.Models.DTOs
{
    /// <summary>
    /// Demande de mise à jour d'un abonnement
    /// </summary>
    public class SubscriptionUpdateRequest
    {
        /// <summary>
        /// Identifiant du nouveau plan ou produit (si changement de plan)
        /// </summary>
        [JsonPropertyName("newPlanId")]
        public string NewPlanId { get; set; } = string.Empty;

        /// <summary>
        /// Nouveau montant en centimes (si ajustement de prix)
        /// </summary>
        [JsonPropertyName("newAmount")]
        public long? NewAmount { get; set; }

        /// <summary>
        /// Quantité d'éléments souscrits (si applicable)
        /// </summary>
        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; } = 1;

        /// <summary>
        /// Définit si l'abonnement doit être annulé à la fin de la période actuelle
        /// </summary>
        [JsonPropertyName("cancelAtPeriodEnd")]
        public bool? CancelAtPeriodEnd { get; set; }

        /// <summary>
        /// Méthode de paiement à utiliser pour cet abonnement
        /// </summary>
        [JsonPropertyName("paymentMethodId")]
        public string PaymentMethodId { get; set; } = string.Empty;

        /// <summary>
        /// Métadonnées supplémentaires pour l'abonnement
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}

using System.Text.Json.Serialization;

namespace PaymentsService.Models.DTOs;

/// <summary>
/// Réponse contenant les informations du PaymentIntent créé
/// </summary>
public class PaymentIntentResponse
{
    /// <summary>
    /// Client secret du PaymentIntent, utilisé côté client pour confirmer le paiement
    /// </summary>
    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// ID du PaymentIntent généré par Stripe
    /// </summary>
    [JsonPropertyName("paymentIntentId")]
    public string PaymentIntentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Clé publique Stripe à utiliser côté client
    /// </summary>
    [JsonPropertyName("publishableKey")]
    public string PublishableKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Montant du paiement (en centimes)
    /// </summary>
    [JsonPropertyName("amount")]
    public long Amount { get; set; }
    
    /// <summary>
    /// Devise du paiement
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PaymentsService.Models.DTOs;

/// <summary>
/// Type de fréquence pour les paiements récurrents
/// </summary>
public enum RecurringFrequency
{
    /// <summary>
    /// Mensuel
    /// </summary>
    Monthly,
    
    /// <summary>
    /// Trimestriel
    /// </summary>
    Quarterly,
    
    /// <summary>
    /// Annuel
    /// </summary>
    Yearly
}

/// <summary>
/// Demande de création d'un PaymentIntent Stripe
/// </summary>
public class PaymentIntentRequest
{
    /// <summary>
    /// Montant à payer (en centimes)
    /// </summary>
    [Required]
    [JsonPropertyName("amount")]
    public long Amount { get; set; }
    
    /// <summary>
    /// Devise (par défaut EUR)
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "eur";
    
    /// <summary>
    /// Description du paiement
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// ID de l'utilisateur qui effectue le paiement
    /// </summary>
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// ID du produit ou service acheté
    /// </summary>
    [JsonPropertyName("productId")]
    public string ProductId { get; set; } = string.Empty;
    
    /// <summary>
    /// Indique si ce paiement fait partie d'un abonnement récurrent
    /// </summary>
    [JsonPropertyName("isRecurring")]
    public bool IsRecurring { get; set; } = false;
    
    /// <summary>
    /// Fréquence de l'abonnement (uniquement valide si IsRecurring est true)
    /// </summary>
    [JsonPropertyName("frequency")]
    public RecurringFrequency? Frequency { get; set; }
}

using System.Text.Json.Serialization;

namespace Frontend.Models;

/// <summary>
/// Modèle représentant une demande de remboursement
/// </summary>
public class RefundRequest
{
    /// <summary>
    /// Identifiant du paiement à rembourser
    /// </summary>
    [JsonPropertyName("paymentId")]
    public string PaymentId { get; set; } = string.Empty;
    
    /// <summary>
    /// Montant à rembourser (null pour un remboursement complet)
    /// </summary>
    [JsonPropertyName("amount")]
    public long? Amount { get; set; }
    
    /// <summary>
    /// Raison du remboursement
    /// </summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

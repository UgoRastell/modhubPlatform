using System.Text.Json.Serialization;

namespace Frontend.Models;

/// <summary>
/// Modèle pour la requête d'échange de code d'autorisation Google
/// </summary>
public class GoogleAuthCodeRequest
{
    /// <summary>
    /// Code d'autorisation reçu de Google OAuth2
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// URI de redirection utilisé lors de la demande initiale
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; } = string.Empty;
    
    /// <summary>
    /// ID client de l'application Google OAuth2
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UsersService.Models.DTOs;

/// <summary>
/// Modèle pour la requête d'échange de code d'autorisation Google OAuth
/// </summary>
public class GoogleAuthCodeRequestDto
{
    /// <summary>
    /// Code d'autorisation reçu de Google
    /// </summary>
    [Required]
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// URI de redirection utilisée lors de la demande initiale
    /// </summary>
    [Required]
    [JsonPropertyName("redirect_uri")]
    public string RedirectUri { get; set; } = string.Empty;
    
    /// <summary>
    /// ID client de l'application Google OAuth
    /// </summary>
    [Required]
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;
}

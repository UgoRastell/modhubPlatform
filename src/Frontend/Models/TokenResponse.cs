using System.Text.Json.Serialization;

namespace Frontend.Models;

public class TokenResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
    
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

using Microsoft.Extensions.Configuration;

namespace Frontend.Services;

/// <summary>
/// Service pour la gestion centralisée des configurations OAuth
/// </summary>
public interface IOAuthConfigService
{
    /// <summary>
    /// Récupère l'ID client Google OAuth
    /// </summary>
    string GetGoogleClientId();
    
    /// <summary>
    /// Récupère l'URI de redirection pour Google OAuth
    /// </summary>
    string GetGoogleRedirectUri();
}

public class OAuthConfigService : IOAuthConfigService
{
    private readonly IConfiguration _configuration;
    private readonly string _googleClientId;
    private readonly string _baseUri;

    public OAuthConfigService(IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Récupérer le client ID depuis la configuration
        _googleClientId = _configuration["OAuth:GoogleClientId"] ?? 
                         "59693042860-ruj3b321ntsr25mt4m8shfi7acpsog83.apps.googleusercontent.com";
                         
        // Récupérer l'URI de base
        _baseUri = _configuration["BaseUri"] ?? "";
    }

    public string GetGoogleClientId() => _googleClientId;
    
    public string GetGoogleRedirectUri() => string.IsNullOrEmpty(_baseUri) 
        ? "https://modhub.ovh/signin-google" 
        : $"{_baseUri.TrimEnd('/')}/signin-google";
}

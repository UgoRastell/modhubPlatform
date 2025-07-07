using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using UsersService.Models.DTOs;
using UsersService.Services;

namespace UsersService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;
    private readonly IConfiguration _configuration;

    public OAuthController(IOAuthService oauthService, ILogger<OAuthController> logger, IConfiguration configuration)
    {
        _oauthService = oauthService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Authentifier un utilisateur via Google OAuth avec un ID token
    /// </summary>
    /// <param name="request">Requête d'authentification externe avec token Google</param>
    /// <returns>Réponse d'authentification avec token JWT</returns>
    [HttpPost("google")]
    public async Task<ActionResult<ExternalAuthResponse>> AuthenticateGoogle([FromBody] ExternalAuthRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            return BadRequest(new { message = "Token ID Google manquant" });
        }

        try
        {
            var response = await _oauthService.AuthenticateExternalUserAsync(request);
            
            if (response == null)
            {
                return BadRequest(new { message = "Échec de l'authentification Google" });
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'authentification Google");
            return StatusCode(500, new { message = "Erreur lors de l'authentification" });
        }
    }

    /// <summary>
    /// Lier un compte Google à un utilisateur existant
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="request">Requête de liaison avec token Google</param>
    /// <returns>Réponse de liaison</returns>
    [Authorize]
    [HttpPost("link/{userId}")]
    public async Task<ActionResult<LinkExternalLoginResponse>> LinkGoogleAccount(string userId, [FromBody] LinkExternalLoginRequest request)
    {
        // Vérifier que l'utilisateur authentifié est bien celui qui fait la demande
        var authenticatedUserId = User.FindFirst("sub")?.Value;
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        try
        {
            var response = await _oauthService.LinkExternalLoginAsync(userId, request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la liaison du compte Google");
            return StatusCode(500, new { message = "Erreur lors de la liaison du compte" });
        }
    }

    /// <summary>
    /// Délier un compte Google d'un utilisateur existant
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Statut de l'opération</returns>
    [Authorize]
    [HttpDelete("unlink/{userId}")]
    public async Task<IActionResult> UnlinkGoogleAccount(string userId, [FromQuery] string provider = "google")
    {
        // Vérifier que l'utilisateur authentifié est bien celui qui fait la demande
        var authenticatedUserId = User.FindFirst("sub")?.Value;
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        try
        {
            var result = await _oauthService.UnlinkExternalLoginAsync(userId, provider);
            
            if (!result)
            {
                return BadRequest(new { message = "Échec de la déliaison du compte Google" });
            }

            return Ok(new { message = "Compte Google délié avec succès" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la déliaison du compte Google");
            return StatusCode(500, new { message = "Erreur lors de la déliaison du compte" });
        }
    }

    /// <summary>
    /// Obtenir la liste des comptes externes liés à un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des comptes externes</returns>
    [Authorize]
    [HttpGet("logins/{userId}")]
    public async Task<ActionResult<List<ExternalLoginInfo>>> GetExternalLogins(string userId)
    {
        // Vérifier que l'utilisateur authentifié est bien celui qui fait la demande
        var authenticatedUserId = User.FindFirst("sub")?.Value;
        if (authenticatedUserId != userId)
        {
            return Forbid();
        }

        try
        {
            var logins = await _oauthService.GetExternalLoginsAsync(userId);
            return Ok(logins);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des comptes externes");
            return StatusCode(500, new { message = "Erreur lors de la récupération des comptes externes" });
        }
    }
    
    /// <summary>
    /// Échange un code d'autorisation Google contre un token d'accès et authentifie l'utilisateur
    /// </summary>
    /// <param name="request">Requête contenant le code d'autorisation</param>
    /// <returns>Réponse d'authentification avec token JWT</returns>
    [Authorize(Policy = "AllowAnonymous")]
    [HttpPost("google-callback")]
    public async Task<ActionResult<ExternalAuthResponse>> GoogleCallback([FromBody] GoogleAuthCodeRequestDto request)
    {
        // Extraire immédiatement les propriétés pour éviter toute ambiguïté
        string authCode = request.Code;
        
        if (string.IsNullOrEmpty(authCode))
        {
            return BadRequest(new { message = "Code d'autorisation manquant" });
        }

        try
        {
            // 1. Échanger le code d'autorisation contre un token d'accès Google
            using var httpClient = new HttpClient();
            
            // Récupérer le client secret depuis la configuration
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];
            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Google client secret is missing in configuration");
                return StatusCode(500, new { message = "Erreur de configuration" });
            }
            
            // Préparer la requête pour échanger le code contre un token
            // authCode est déjà extrait ci-dessus
            string clientId = request?.ClientId ?? string.Empty;
            string redirectUri = request?.RedirectUri ?? string.Empty;
            
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "code", authCode },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUri },
                { "grant_type", "authorization_code" }
            });
            
            // Envoyer la requête à l'API Google
            var tokenResponse = await httpClient.PostAsync(
                "https://oauth2.googleapis.com/token", 
                tokenRequestContent);
                
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var errorContent = await tokenResponse.Content.ReadAsStringAsync();
                _logger.LogError("Google token exchange failed: {Error}", errorContent);
                return BadRequest(new { message = "Échec de l'échange du code d'autorisation" });
            }
            
            // Extraire le token d'accès et les informations d'ID token
            var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
            var googleTokens = JsonSerializer.Deserialize<JsonElement>(tokenResponseContent);
            
            if (!googleTokens.TryGetProperty("id_token", out var idTokenElement))
            {
                return BadRequest(new { message = "ID token manquant dans la réponse de Google" });
            }
            
            var idToken = idTokenElement.GetString() ?? string.Empty;
            
            // 2. Utiliser l'ID token pour authentifier l'utilisateur
            var authRequest = new ExternalAuthRequest
            {
                IdToken = idToken,
                Provider = "google"
            };
            
            var authResponse = await _oauthService.AuthenticateExternalUserAsync(authRequest);
            
            if (authResponse == null)
            {
                return BadRequest(new { message = "Échec de l'authentification avec Google" });
            }
            
            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'authentification avec le code Google");
            return StatusCode(500, new { message = "Erreur lors de l'authentification" });
        }
    }
}

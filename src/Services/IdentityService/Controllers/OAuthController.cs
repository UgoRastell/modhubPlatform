using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IdentityService.Controllers
{
    [Route("api/OAuth")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly IOAuthService _oauthService;
        private readonly IRabbitMQService _rabbitMQService;
        private readonly ILogger<OAuthController> _logger;
        private readonly IConfiguration _configuration;

        public OAuthController(
            IOAuthService oauthService,
            IRabbitMQService rabbitMQService,
            ILogger<OAuthController> logger,
            IConfiguration configuration)
        {
            _oauthService = oauthService;
            _rabbitMQService = rabbitMQService;
            _logger = logger;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet("google-callback")]
        [AllowAnonymous]
        [HttpPost("google-callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                _logger.LogInformation("Google OAuth callback received with code: {CodeLength}", code?.Length ?? 0);
                
                // Échanger le code d'autorisation contre un token d'accès
                var tokenResponse = await _oauthService.ExchangeGoogleAuthCodeForTokenAsync(code);
                
                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    _logger.LogError("Failed to exchange Google auth code for token");
                    return BadRequest(new { message = "Échec de l'authentification Google OAuth." });
                }
                
                // Obtenir les informations utilisateur depuis le token
                var userInfo = await _oauthService.GetGoogleUserInfoAsync(tokenResponse.AccessToken);
                
                if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
                {
                    _logger.LogError("Failed to get user info from Google token");
                    return BadRequest(new { message = "Impossible de récupérer les informations utilisateur." });
                }
                
                // Authentifier ou créer l'utilisateur localement
                var result = await _oauthService.AuthenticateGoogleUserAsync(userInfo);
                
                // Publier l'événement approprié
                if (result.IsNewUser)
                {
                    await _rabbitMQService.PublishAsync("user.events", new UserCreatedEvent
                    {
                        UserId = result.UserId,
                        Username = result.Username,
                        Email = userInfo.Email,
                        CreatedAt = DateTime.UtcNow,
                        OAuthProvider = "google"
                    });
                }
                
                // Événement de connexion
                await _rabbitMQService.PublishAsync("user.events", new UserLoggedInEvent
                {
                    UserId = result.UserId,
                    Username = result.Username,
                    LoginTime = DateTime.UtcNow,
                    OAuthProvider = "google"
                });

                // Générer un URL avec le token JWT pour rediriger vers l'application frontend
                var frontendUrl = _configuration["FrontendUrl"] ?? "https://modhub.ovh";
                var returnUrl = "/";
                
                // Extraire l'URL de retour depuis le state si présent
                if (!string.IsNullOrEmpty(state) && state.Contains('|'))
                {
                    var parts = state.Split('|');
                    if (parts.Length > 1)
                    {
                        returnUrl = parts[1];
                    }
                }
                
                // Construire l'URL de redirection avec le token et autres informations
                var redirectUrl = $"{frontendUrl.TrimEnd('/')}/oauth-callback" +
                                  $"?token={Uri.EscapeDataString(result.Token)}" +
                                  $"&refreshToken={Uri.EscapeDataString(result.RefreshToken)}" +
                                  $"&userId={Uri.EscapeDataString(result.UserId)}" +
                                  $"&username={Uri.EscapeDataString(result.Username)}" +
                                  $"&returnUrl={Uri.EscapeDataString(returnUrl)}";

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth callback");
                // Rediriger vers une page d'erreur dans l'application frontend
                return Redirect($"{(_configuration["FrontendUrl"] ?? "https://modhub.ovh")}/auth-error?error={Uri.EscapeDataString("Une erreur s'est produite lors de l'authentification Google.")}");
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UsersService.Models.DTOs;
using UsersService.Services;

namespace UsersService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OAuthController : ControllerBase
{
    private readonly IOAuthService _oauthService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(IOAuthService oauthService, ILogger<OAuthController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <summary>
    /// Authentifier un utilisateur via Google OAuth
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
}

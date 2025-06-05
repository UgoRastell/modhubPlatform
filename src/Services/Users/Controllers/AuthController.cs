using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UsersService.Models.DTOs;
using UsersService.Services;

namespace UsersService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            _logger.LogWarning("Échec de connexion pour {Email}", request.Email);
            return Unauthorized(new { message = "Email ou mot de passe incorrect" });
        }

        _logger.LogInformation("Connexion réussie pour {Email}", request.Email);
        return Ok(response);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validation de base réalisée par les attributs de données dans RegisterRequest
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var response = await _authService.RegisterAsync(request);
        if (response == null)
        {
            return BadRequest(new { message = "L'inscription a échoué. L'email ou le nom d'utilisateur pourrait déjà exister." });
        }

        _logger.LogInformation("Nouvel utilisateur enregistré avec l'email {Email}", request.Email);
        return CreatedAtAction(nameof(Login), new { id = response.User.Id }, response);
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request.RefreshToken);
        if (response == null)
        {
            _logger.LogWarning("Échec de rafraîchissement du token");
            return Unauthorized(new { message = "Token de rafraîchissement invalide" });
        }

        _logger.LogInformation("Token rafraîchi avec succès");
        return Ok(response);
    }

    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "L'email est requis" });
        }

        await _authService.RequestPasswordResetAsync(request.Email);

        // Pour des raisons de sécurité, on retourne toujours une réponse positive
        // même si l'email n'existe pas dans notre base de données
        return Ok(new { message = "Si votre email existe dans notre système, vous recevrez un lien de réinitialisation." });
    }

    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ConfirmResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _authService.ResetPasswordAsync(request.Token, request.Password);
        if (!success)
        {
            return BadRequest(new { message = "Le token de réinitialisation est invalide ou a expiré." });
        }

        _logger.LogInformation("Mot de passe réinitialisé avec succès");
        return Ok(new { message = "Votre mot de passe a été réinitialisé avec succès." });
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        await _authService.RevokeTokenAsync(userId);
        _logger.LogInformation("Utilisateur déconnecté: {UserId}", userId);
        
        return Ok(new { message = "Déconnexion réussie" });
    }
}

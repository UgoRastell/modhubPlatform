using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UsersService.Models.DTOs;
using UsersService.Services;

namespace UsersService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmailVerificationController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<EmailVerificationController> _logger;

    public EmailVerificationController(IAuthService authService, ILogger<EmailVerificationController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Envoie un email de vérification à l'utilisateur
    /// </summary>
    /// <param name="request">Requête de renvoi d'email de vérification</param>
    /// <returns>Statut de l'opération</returns>
    [HttpPost("send")]
    [AllowAnonymous]
    public async Task<ActionResult<EmailResponse>> SendVerificationEmail([FromBody] ResendVerificationEmailRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new EmailResponse 
            { 
                Success = false, 
                Message = "Email requis" 
            });
        }

        try
        {
            bool result = await _authService.SendEmailVerificationAsync(request.Email);

            if (result)
            {
                return Ok(new EmailResponse
                {
                    Success = true,
                    Message = "Email de vérification envoyé avec succès"
                });
            }
            else
            {
                // Ne pas exposer si l'email existe ou pas pour des raisons de sécurité
                return Ok(new EmailResponse
                {
                    Success = true,
                    Message = "Si l'email existe, un lien de vérification a été envoyé"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'envoi de l'email de vérification pour {Email}", request.Email);
            return StatusCode(500, new EmailResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de l'envoi de l'email de vérification"
            });
        }
    }

    /// <summary>
    /// Vérifie l'email d'un utilisateur à l'aide d'un token
    /// </summary>
    /// <param name="request">Requête de vérification d'email</param>
    /// <returns>Statut de l'opération</returns>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<ActionResult<EmailResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token))
        {
            return BadRequest(new EmailResponse
            {
                Success = false,
                Message = "Email et token requis"
            });
        }

        try
        {
            bool result = await _authService.VerifyEmailAsync(request.Email, request.Token);

            if (result)
            {
                return Ok(new EmailResponse
                {
                    Success = true,
                    Message = "Email vérifié avec succès"
                });
            }
            else
            {
                return BadRequest(new EmailResponse
                {
                    Success = false,
                    Message = "Le token de vérification est invalide ou expiré"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la vérification de l'email pour {Email}", request.Email);
            return StatusCode(500, new EmailResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la vérification de l'email"
            });
        }
    }

    /// <summary>
    /// Vérifie le statut de vérification de l'email d'un utilisateur
    /// </summary>
    /// <returns>Statut de vérification de l'email</returns>
    [HttpGet("status")]
    [Authorize]
    public ActionResult<EmailResponse> GetEmailVerificationStatus()
    {
        var emailVerified = User.HasClaim(c => c.Type == "email_verified" && c.Value == "true");
        var email = User.FindFirst("email")?.Value;

        return Ok(new EmailResponse
        {
            Success = true,
            Message = emailVerified ? "Email vérifié" : "Email non vérifié",
            Email = email,
            EmailVerified = emailVerified
        });
    }
}

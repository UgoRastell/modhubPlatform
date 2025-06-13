using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using UsersService.Models.DTOs;
using UsersService.Services;

namespace UsersService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PasswordResetController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(IAuthService authService, ILogger<PasswordResetController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Demande de réinitialisation du mot de passe
    /// </summary>
    /// <param name="request">Email de l'utilisateur</param>
    /// <returns>Statut de l'opération</returns>
    [HttpPost("request")]
    [AllowAnonymous]
    public async Task<ActionResult<PasswordResetResponse>> RequestPasswordReset([FromBody] InitiatePasswordResetRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new PasswordResetResponse
            {
                Success = false,
                Message = "Email requis"
            });
        }

        try
        {
            // Appel du service de réinitialisation
            await _authService.RequestPasswordResetAsync(request.Email);

            // Pour des raisons de sécurité, toujours retourner un succès même si l'email n'existe pas
            return Ok(new PasswordResetResponse
            {
                Success = true,
                Message = "Si l'email existe, un lien de réinitialisation a été envoyé"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la demande de réinitialisation du mot de passe pour {Email}", request.Email);
            return StatusCode(500, new PasswordResetResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la demande de réinitialisation"
            });
        }
    }

    /// <summary>
    /// Réinitialisation du mot de passe avec token
    /// </summary>
    /// <param name="request">Email, token et nouveau mot de passe</param>
    /// <returns>Statut de l'opération</returns>
    [HttpPost("reset")]
    [AllowAnonymous]
    public async Task<ActionResult<PasswordResetResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Token) || 
            string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest(new PasswordResetResponse
            {
                Success = false,
                Message = "Email, token et nouveau mot de passe requis"
            });
        }

        try
        {
            bool result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

            if (result)
            {
                return Ok(new PasswordResetResponse
                {
                    Success = true,
                    Message = "Mot de passe réinitialisé avec succès"
                });
            }
            else
            {
                return BadRequest(new PasswordResetResponse
                {
                    Success = false,
                    Message = "Le token de réinitialisation est invalide ou expiré"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation du mot de passe pour {Email}", request.Email);
            return StatusCode(500, new PasswordResetResponse
            {
                Success = false,
                Message = "Une erreur est survenue lors de la réinitialisation du mot de passe"
            });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UsersService.Models.DTOs;
using UsersService.Services;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace UsersService.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var profile = await _userService.GetProfileAsync(userId);
        if (profile == null)
        {
            return NotFound(new { message = "Profil non trouvé" });
        }

        return Ok(profile);
    }

    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var updatedProfile = await _userService.UpdateProfileAsync(userId, request);
        if (updatedProfile == null)
        {
            return BadRequest(new { message = "La mise à jour du profil a échoué. Le nom d'utilisateur pourrait déjà être utilisé." });
        }

        _logger.LogInformation("Profil mis à jour pour l'utilisateur: {UserId}", userId);
        return Ok(updatedProfile);
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var success = await _userService.UpdatePasswordAsync(userId, request);
        if (!success)
        {
            return BadRequest(new { message = "La mise à jour du mot de passe a échoué. Vérifiez que votre mot de passe actuel est correct." });
        }

        _logger.LogInformation("Mot de passe mis à jour pour l'utilisateur: {UserId}", userId);
        return Ok(new { message = "Mot de passe mis à jour avec succès" });
    }

    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var success = await _userService.UpdatePreferencesAsync(userId, request);
        if (!success)
        {
            return BadRequest(new { message = "La mise à jour des préférences a échoué." });
        }

        _logger.LogInformation("Préférences mises à jour pour l'utilisateur: {UserId}", userId);
        return Ok(new { message = "Préférences mises à jour avec succès" });
    }

    // Endpoints RGPD

    [HttpPut("consent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateConsent([FromBody] GdprConsentUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var success = await _userService.UpdateConsentAsync(userId, request);
        if (!success)
        {
            return BadRequest(new { message = "La mise à jour du consentement a échoué." });
        }

        _logger.LogInformation("Consentement {ConsentKey} mis à jour pour l'utilisateur: {UserId}", 
            request.ConsentKey, userId);
        return Ok(new { message = "Consentement mis à jour avec succès" });
    }

    [HttpPost("export")]
    [ProducesResponseType(typeof(GdprExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportUserData([FromBody] GdprExportRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var exportData = await _userService.ExportUserDataAsync(userId, request.Password);
        if (exportData == null)
        {
            return BadRequest(new { message = "L'exportation des données a échoué. Vérifiez que votre mot de passe est correct." });
        }

        _logger.LogInformation("Données exportées pour l'utilisateur: {UserId}", userId);
        return Ok(exportData);
    }

    [HttpPost("avatar")]
    [ProducesResponseType(typeof(AvatarUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Aucun fichier n'a été envoyé" });
        }

        if (file.Length > 2 * 1024 * 1024) // 2MB max
        {
            return BadRequest(new { message = "La taille du fichier ne doit pas dépasser 2 MB" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
        {
            return BadRequest(new { message = "Seuls les formats JPG et PNG sont acceptés" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var result = await _userService.UploadAvatarAsync(userId, file);
        if (result == null)
        {
            return BadRequest(new { message = "L'upload de l'avatar a échoué" });
        }

        _logger.LogInformation("Avatar mis à jour pour l'utilisateur: {UserId}", userId);
        return Ok(result);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteUserData([FromBody] GdprDeleteRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié" });
        }

        var success = await _userService.DeleteUserDataAsync(userId, request);
        if (!success)
        {
            return BadRequest(new { message = "La suppression du compte a échoué. Vérifiez que votre mot de passe est correct et que vous avez confirmé la suppression." });
        }

        _logger.LogInformation("Compte supprimé pour l'utilisateur: {UserId}, Raison: {Reason}", 
            userId, request.Reason);
        return Ok(new { message = "Votre compte et vos données ont été supprimés avec succès." });
    }
}

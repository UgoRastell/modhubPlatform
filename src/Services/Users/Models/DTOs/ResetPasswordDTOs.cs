using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

/// <summary>
/// Demande initiale de réinitialisation de mot de passe (envoi du mail)
/// </summary>
public class InitiatePasswordResetRequest
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = null!;
}

/// <summary>
/// Demande de réinitialisation du mot de passe avec token
/// </summary>
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = null!;
    
    [Required(ErrorMessage = "Le token est obligatoire")]
    public string Token { get; set; } = null!;
    
    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Réponse à une demande de réinitialisation de mot de passe
/// </summary>
public class PasswordResetResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

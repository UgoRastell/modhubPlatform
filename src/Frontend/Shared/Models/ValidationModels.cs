using System.ComponentModel.DataAnnotations;

namespace Frontend.Shared.Models;

/// <summary>
/// Modèle de validation pour le formulaire de connexion
/// </summary>
public class LoginModel
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

/// <summary>
/// Modèle de validation pour le formulaire d'inscription
/// </summary>
public class RegisterModel
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [StringLength(50, ErrorMessage = "Le nom d'utilisateur doit contenir entre {2} et {1} caractères", MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vous devez accepter les conditions d'utilisation")]
    public bool AcceptTerms { get; set; }
}

/// <summary>
/// Modèle de validation pour la demande de réinitialisation de mot de passe
/// </summary>
public class ResetPasswordRequestModel
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Modèle de validation pour la réinitialisation de mot de passe
/// </summary>
public class ResetPasswordModel
{
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [StringLength(100, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères", MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    public string Token { get; set; } = string.Empty;
}

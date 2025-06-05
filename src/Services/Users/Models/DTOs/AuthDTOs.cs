using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
    public string Password { get; set; } = null!;
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit comporter au moins 3 caractères")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Le nom d'utilisateur ne peut contenir que des lettres, des chiffres et des underscores")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Le mot de passe doit contenir au moins une lettre minuscule, une lettre majuscule, un chiffre et un caractère spécial")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "L'acceptation des conditions d'utilisation est requise")]
    public bool AcceptTerms { get; set; }

    // Champs optionnels
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Consentements RGPD spécifiques
    public bool? ConsentMarketing { get; set; }
    public bool? ConsentDataAnalytics { get; set; }
    public bool? ConsentThirdParty { get; set; }
}

public class TokenResponse
{
    public string Token { get; set; } = null!;
    public DateTime Expires { get; set; }
    public string RefreshToken { get; set; } = null!;
    public UserBasicInfo User { get; set; } = null!;
}

public class UserBasicInfo
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public List<string> Roles { get; set; } = null!;
    public string? ProfilePictureUrl { get; set; }
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Format d'email invalide")]
    public string Email { get; set; } = null!;
}

public class ConfirmResetPasswordRequest
{
    [Required(ErrorMessage = "Le jeton est requis")]
    public string Token { get; set; } = null!;

    [Required(ErrorMessage = "Le mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Le mot de passe doit contenir au moins une lettre minuscule, une lettre majuscule, un chiffre et un caractère spécial")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class UpdateProfileRequest
{
    [MinLength(3, ErrorMessage = "Le nom d'utilisateur doit comporter au moins 3 caractères")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Le nom d'utilisateur ne peut contenir que des lettres, des chiffres et des underscores")]
    public string? Username { get; set; }

    public string? FirstName { get; set; }
    
    public string? LastName { get; set; }
    
    public string? Bio { get; set; }
    
    public string? ProfilePictureUrl { get; set; }
}

public class UpdatePasswordRequest
{
    [Required(ErrorMessage = "Le mot de passe actuel est requis")]
    public string CurrentPassword { get; set; } = null!;
    
    [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
    [MinLength(8, ErrorMessage = "Le mot de passe doit comporter au moins 8 caractères")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Le mot de passe doit contenir au moins une lettre minuscule, une lettre majuscule, un chiffre et un caractère spécial")]
    public string NewPassword { get; set; } = null!;
    
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = null!;
}

public class UpdatePreferencesRequest
{
    public bool? EmailNotifications { get; set; }
    public bool? MarketingEmails { get; set; }
    public string? Theme { get; set; }
    public string? Language { get; set; }
}

// DTOs pour les fonctionnalités RGPD
public class GdprConsentUpdateRequest
{
    [Required(ErrorMessage = "L'identifiant du consentement est requis")]
    public string ConsentKey { get; set; } = null!;
    
    [Required(ErrorMessage = "Le statut du consentement est requis")]
    public bool Consented { get; set; }
}

public class GdprExportRequest
{
    [Required(ErrorMessage = "Le mot de passe est requis pour confirmer l'identité")]
    public string Password { get; set; } = null!;
}

public class GdprDeleteRequest
{
    [Required(ErrorMessage = "Le mot de passe est requis pour confirmer la suppression du compte")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "Confirmation de suppression requise")]
    public bool ConfirmDeletion { get; set; }
    
    [Required(ErrorMessage = "Raison de la suppression requise")]
    public string Reason { get; set; } = null!;
}

public class UserProfileResponse
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public List<string> Roles { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public UserPreferences Preferences { get; set; } = null!;
}

public class GdprExportResponse
{
    public UserProfileResponse Profile { get; set; } = null!;
    public Dictionary<string, DataConsent> Consents { get; set; } = null!;
    public List<UserActivity> Activities { get; set; } = null!;
    public DateTime ExportDate { get; set; } = DateTime.UtcNow;
}

public class UserActivity
{
    public string Type { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

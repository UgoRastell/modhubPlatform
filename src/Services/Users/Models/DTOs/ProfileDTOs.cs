using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class UserPreferencesDTO
{
    public bool EmailNotifications { get; set; } = true;
    public bool MarketingEmails { get; set; } = false;
    public string Theme { get; set; } = "light";
    public string Language { get; set; } = "fr";
}

public class UpdateAvatarResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AvatarUrl { get; set; }
}

public class RgpdExportResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserDataExport? Data { get; set; }
}

public class UserDataExport
{
    public string Id { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public Dictionary<string, DataConsent> DataConsents { get; set; } = new Dictionary<string, DataConsent>();
    public List<string> Roles { get; set; } = new List<string>();
    public Dictionary<string, object>? AdditionalData { get; set; }
}

public class DeleteAccountRequest
{
    [Required(ErrorMessage = "Le mot de passe est requis pour confirmer la suppression du compte")]
    public string Password { get; set; } = null!;
    
    [Required(ErrorMessage = "La confirmation est requise")]
    public bool Confirmation { get; set; }
}

public class DeleteAccountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
}

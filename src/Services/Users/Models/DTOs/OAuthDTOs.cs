using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class ExternalAuthRequest
{
    [Required]
    public string Provider { get; set; } = null!; // "google", "facebook", etc.

    [Required]
    public string IdToken { get; set; } = null!;
}

public class GoogleUserInfo
{
    // Informations récupérées depuis Google
    public string Id { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool VerifiedEmail { get; set; }
    public string Name { get; set; } = null!;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
}

public class ExternalAuthResponse
{
    public TokenResponse TokenResponse { get; set; } = null!;
    public bool IsNewUser { get; set; }
}

public class LinkExternalLoginRequest
{
    [Required]
    public string Provider { get; set; } = null!;

    [Required]
    public string IdToken { get; set; } = null!;
}

public class LinkExternalLoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public List<ExternalLoginInfo>? LinkedAccounts { get; set; }
}

public class ExternalLoginInfo
{
    public string Provider { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public DateTime LinkedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Le jeton de vérification est requis")]
    public string Token { get; set; } = null!;
    
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Le format de l'email est invalide")]
    public string Email { get; set; } = null!;
}

public class ResendVerificationEmailRequest
{
    [Required(ErrorMessage = "L'email est requis")]
    [EmailAddress(ErrorMessage = "Le format de l'email est invalide")]
    public string Email { get; set; } = null!;
}

public class EmailResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
}

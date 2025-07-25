using System.ComponentModel.DataAnnotations;

namespace UsersService.Models.DTOs;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

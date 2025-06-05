using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

/// <summary>
/// Représente un modèle utilisateur pour l'interface
/// </summary>
public class UserModel
{
    public string Id { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ProfilePictureUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLogin { get; set; }

    public List<string> Roles { get; set; } = new List<string>();

    public bool IsPremium { get; set; }
}

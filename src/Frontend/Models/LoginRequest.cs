using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

/// <summary>
/// Modèle pour la demande de connexion
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Email de l'utilisateur
    /// </summary>
    [Required(ErrorMessage = "L'adresse email est requise")]
    [EmailAddress(ErrorMessage = "Le format de l'adresse email est invalide")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Mot de passe
    /// </summary>
    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Se souvenir de moi
    /// </summary>
    public bool RememberMe { get; set; }
}

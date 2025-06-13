using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

/// <summary>
/// Modèle pour la demande d'inscription
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Nom d'utilisateur
    /// </summary>
    [Required(ErrorMessage = "Le nom d'utilisateur est requis")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Le nom d'utilisateur doit contenir entre {2} et {1} caractères")]
    public string Username { get; set; } = string.Empty;
    
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
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Le mot de passe doit contenir au moins une lettre minuscule, une lettre majuscule, un chiffre et un caractère spécial")]
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Confirmation du mot de passe
    /// </summary>
    [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
    [Compare(nameof(Password), ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Acceptation des conditions d'utilisation
    /// </summary>
    [Range(typeof(bool), "true", "true", ErrorMessage = "Vous devez accepter les conditions d'utilisation")]
    public bool AcceptTerms { get; set; }
}

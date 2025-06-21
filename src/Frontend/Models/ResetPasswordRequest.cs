using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    /// <summary>
    /// Modèle pour la demande de réinitialisation de mot de passe
    /// </summary>
    public class ResetPasswordRequest
    {
        /// <summary>
        /// Email de l'utilisateur
        /// </summary>
        [Required(ErrorMessage = "L'adresse email est requise")]
        [EmailAddress(ErrorMessage = "Le format de l'adresse email est invalide")]
        public string Email { get; set; } = string.Empty;
        
        /// <summary>
        /// Token de réinitialisation
        /// </summary>
        [Required(ErrorMessage = "Le token est requis")]
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// Nouveau mot de passe
        /// </summary>
        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins {2} caractères")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Le mot de passe doit contenir au moins une lettre minuscule, une lettre majuscule, un chiffre et un caractère spécial")]
        public string Password { get; set; } = string.Empty;
        
        /// <summary>
        /// Confirmation du nouveau mot de passe
        /// </summary>
        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare(nameof(Password), ErrorMessage = "Le mot de passe et sa confirmation ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Frontend.Models
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Le mot de passe actuel est requis")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
            ErrorMessage = "Le mot de passe doit contenir au moins une minuscule, une majuscule, un chiffre et un caractère spécial")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("NewPassword", ErrorMessage = "Le nouveau mot de passe et sa confirmation ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

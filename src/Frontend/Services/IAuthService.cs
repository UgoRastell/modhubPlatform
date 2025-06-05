using Frontend.Models;
using System.Threading.Tasks;

namespace Frontend.Services;

/// <summary>
/// Interface du service d'authentification
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authentification d'un utilisateur
    /// </summary>
    Task<ApiResponse<string>> Login(LoginRequest loginModel);
    
    /// <summary>
    /// Inscription d'un nouvel utilisateur
    /// </summary>
    Task<ApiResponse<string>> Register(RegisterRequest registerModel);
    
    /// <summary>
    /// Déconnexion de l'utilisateur courant
    /// </summary>
    Task Logout();
    
    /// <summary>
    /// Demande de réinitialisation de mot de passe
    /// </summary>
    Task<ApiResponse<bool>> RequestPasswordReset(string email);
    
    /// <summary>
    /// Réinitialisation du mot de passe avec un token
    /// </summary>
    Task<ApiResponse<bool>> ResetPassword(ResetPasswordRequest model);
    
    /// <summary>
    /// Obtention des informations de l'utilisateur courant
    /// </summary>
    Task<UserProfile?> GetCurrentUser();
}

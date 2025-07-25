using Frontend.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Collections.Generic;

namespace Frontend.Services;

/// <summary>
/// Extension pour ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
    {
        var claim = principal.FindFirst(claimType);
        return claim?.Value;
    }
}

/// <summary>
/// Implémentation du service d'authentification
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IEmailService _emailService;

    public AuthService(
        HttpClient httpClient, 
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        IEmailService emailService)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _emailService = emailService;
    }

    /// <summary>
    /// Authentification d'un utilisateur
    /// </summary>
    public async Task<ApiResponse<string>> Login(LoginRequest loginModel)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    await _localStorage.SetItemAsync("authToken", tokenResponse.Token);
                    
                    // Mettre à jour le header d'authentification pour les futures requêtes
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
                    
                    // Notifier le changement d'état d'authentification
                    ((JwtAuthenticationStateProvider)_authStateProvider)
                        .MarkUserAsAuthenticated(tokenResponse.Token);
                    
                    if (loginModel.RememberMe)
                    {
                        await _localStorage.SetItemAsync("refreshToken", tokenResponse.RefreshToken);
                    }
                    
                    return new ApiResponse<string> { 
                        Success = true, 
                        Data = tokenResponse.Token,
                        Message = "Authentification réussie" 
                    };
                }
            }
            
            string errorMessage = "Échec de l'authentification";
            if (response.Content != null)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errorContent != null && errorContent.ContainsKey("message"))
                {
                    errorMessage = errorContent["message"];
                }
            }
            
            return new ApiResponse<string> { Success = false, Message = errorMessage };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { Success = false, Message = $"Erreur: {ex.Message}" };
        }
    }

    /// <summary>
    /// Inscription d'un nouvel utilisateur
    /// </summary>
    public async Task<ApiResponse<string>> Register(RegisterRequest registerModel)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerModel);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
                
                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    // Dans le cas d'une inscription qui connecte immédiatement l'utilisateur
                    await _localStorage.SetItemAsync("authToken", tokenResponse.Token);
                    
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new AuthenticationHeaderValue("Bearer", tokenResponse.Token);
                    
                    ((JwtAuthenticationStateProvider)_authStateProvider)
                        .MarkUserAsAuthenticated(tokenResponse.Token);
                        
                    return new ApiResponse<string> { 
                        Success = true, 
                        Data = tokenResponse.Token,
                        Message = "Inscription réussie" 
                    };
                }
                
                // Si l'inscription ne connecte pas automatiquement
                return new ApiResponse<string> { 
                    Success = true, 
                    Message = "Inscription réussie. Vous pouvez maintenant vous connecter."
                };
            }
            
            string errorMessage = "Échec de l'inscription";
            if (response.Content != null)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errorContent != null && errorContent.ContainsKey("message"))
                {
                    errorMessage = errorContent["message"];
                }
            }
            
            return new ApiResponse<string> { Success = false, Message = errorMessage };
        }
        catch (Exception ex)
        {
            return new ApiResponse<string> { Success = false, Message = $"Erreur: {ex.Message}" };
        }
    }

    /// <summary>
    /// Déconnexion de l'utilisateur courant
    /// </summary>
    public async Task Logout()
    {
        try
        {
            // Si vous avez un endpoint de déconnexion côté serveur
            var authToken = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", authToken);
                
                await _httpClient.PostAsync("api/auth/logout", null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la déconnexion: {ex.Message}");
        }
        finally
        {
            // Supprimer les tokens stockés localement
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("refreshToken");
            
            // Réinitialiser l'en-tête d'autorisation
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            // Notifier le changement d'état d'authentification
            ((JwtAuthenticationStateProvider)_authStateProvider).MarkUserAsLoggedOut();
        }
    }

    /// <summary>
    /// Demande de réinitialisation de mot de passe
    /// </summary>
    public async Task<ApiResponse<bool>> RequestPasswordReset(string email)
    {
        try
        {
            // Appel à l'API pour vérifier que l'email existe
            var response = await _httpClient.PostAsJsonAsync("api/passwordreset/request", new { Email = email });

            if (response.IsSuccessStatusCode)
            {
                // Génération d'un token de réinitialisation (dans une application réelle, ce token
                // serait généré côté serveur et stocké en base de données)
                var resetToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                
                // Dans une vraie application, stocker ce token en base avec une date d'expiration
                var resetLink = $"https://modhub.com/reset-password?email={Uri.EscapeDataString(email)}&token={resetToken}";
                
                // Construction du contenu du mail
                var subject = "Réinitialisation de votre mot de passe ModHub";
                var htmlMessage = $@"<html>
                    <body style=""font-family: Arial, sans-serif; line-height: 1.6;"">
                        <div style=""max-width: 600px; margin: 0 auto; padding: 20px;"">
                            <h2 style=""color: #00aaff;"">Réinitialisation de mot de passe</h2>
                            <p>Bonjour,</p>
                            <p>Vous avez demandé la réinitialisation de votre mot de passe sur la plateforme ModHub.</p>
                            <p>Veuillez cliquer sur le lien ci-dessous pour définir un nouveau mot de passe:</p>
                            <p style=""text-align: center;"">
                                <a href=""{resetLink}"" style=""display: inline-block; background-color: #00aaff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px;"">Réinitialiser mon mot de passe</a>
                            </p>
                            <p>Ce lien expire dans 24 heures. Si vous n'avez pas demandé cette réinitialisation, vous pouvez ignorer cet email.</p>
                            <p>Cordialement,<br>L'équipe ModHub</p>
                        </div>
                    </body>
                </html>";
                
                // Envoi de l'email via notre service d'email
                var emailSent = await _emailService.SendEmailAsync(email, subject, htmlMessage);
                
                if (emailSent)
                {
                    return new ApiResponse<bool> { Success = true, Data = true, Message = "Instructions de réinitialisation envoyées par email" };
                }
                return new ApiResponse<bool> { Success = false, Data = false, Message = "Impossible d'envoyer l'email de réinitialisation" };
            }
            
            string errorMessage = "Échec de la demande de réinitialisation";
            if (response.Content != null)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errorContent != null && errorContent.ContainsKey("message"))
                {
                    errorMessage = errorContent["message"];
                }
            }
            
            return new ApiResponse<bool> { Success = false, Data = false, Message = errorMessage };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Data = false, Message = $"Erreur: {ex.Message}" };
        }
    }

    /// <summary>
    /// Réinitialisation du mot de passe avec un token
    /// </summary>
    public async Task<ApiResponse<bool>> ResetPassword(ResetPasswordRequest model)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/passwordreset/reset", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool> { Success = true, Data = true, Message = "Réinitialisation du mot de passe réussie" };
            }
            
            string errorMessage = "Échec de la réinitialisation du mot de passe";
            if (response.Content != null)
            {
                var errorContent = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (errorContent != null && errorContent.ContainsKey("message"))
                {
                    errorMessage = errorContent["message"];
                }
            }
            
            return new ApiResponse<bool> { Success = false, Data = false, Message = errorMessage };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Data = false, Message = $"Erreur: {ex.Message}" };
        }
    }

    /// <summary>
    /// Obtention des informations de l'utilisateur courant
    /// </summary>
    public async Task<UserProfile?> GetCurrentUser()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Dans une implémentation réelle, vous pourriez récupérer plus de détails depuis l'API
            return new UserProfile
            {
                Id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                Username = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Roles = user.Claims.Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value).ToArray()
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Vérifie si un utilisateur est connecté
    /// </summary>
    /// <returns>True si un utilisateur est connecté, sinon false</returns>
    public async Task<bool> IsUserLoggedInAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            
            return user.Identity?.IsAuthenticated == true;
        }
        catch
        {
            return false;
        }
    }
}

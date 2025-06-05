using Frontend.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;
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
    private readonly AuthenticationStateProvider _authStateProvider;

    public AuthService(HttpClient httpClient, AuthenticationStateProvider authStateProvider)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
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
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return result ?? new ApiResponse<string> { Success = true, Message = "Authentification réussie" };
            }
            
            return new ApiResponse<string> { Success = false, Message = "Échec de l'authentification" };
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
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return result ?? new ApiResponse<string> { Success = true, Message = "Inscription réussie" };
            }
            
            return new ApiResponse<string> { Success = false, Message = "Échec de l'inscription" };
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
        // Dans une implémentation réelle, vous appelleriez l'API pour invalider le token
        // et mettriez à jour l'état d'authentification
        await Task.CompletedTask;
    }

    /// <summary>
    /// Demande de réinitialisation de mot de passe
    /// </summary>
    public async Task<ApiResponse<bool>> RequestPasswordReset(string email)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password-request", new { Email = email });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool> { Success = true, Data = true, Message = "Demande de réinitialisation envoyée" };
            }
            
            return new ApiResponse<bool> { Success = false, Data = false, Message = "Échec de la demande de réinitialisation" };
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
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", model);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                return result ?? new ApiResponse<bool> { Success = true, Data = true, Message = "Réinitialisation du mot de passe réussie" };
            }
            
            return new ApiResponse<bool> { Success = false, Data = false, Message = "Échec de la réinitialisation du mot de passe" };
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
}

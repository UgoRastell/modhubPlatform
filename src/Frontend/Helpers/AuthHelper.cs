using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Helpers;

/// <summary>
/// Helper pour les opérations liées à l'authentification
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
/// Helper pour les opérations liées à l'authentification
/// </summary>
public class AuthHelper
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AuthHelper(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    /// <summary>
    /// Vérifie si l'utilisateur est connecté
    /// </summary>
    public async Task<bool> IsUserAuthenticated()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }

    /// <summary>
    /// Récupère l'ID de l'utilisateur connecté
    /// </summary>
    public async Task<string?> GetUserId()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return GetUserId(authState);
    }

    /// <summary>
    /// Récupère l'email de l'utilisateur connecté
    /// </summary>
    public async Task<string?> GetUserEmail()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.Email);
    }

    /// <summary>
    /// Vérifie si l'utilisateur a un rôle spécifique
    /// </summary>
    public async Task<bool> IsInRole(string role)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return IsInRole(authState, role);
    }

    /// <summary>
    /// Récupère l'Id de l'utilisateur connecté depuis AuthState
    /// </summary>
    public static string? GetUserId(AuthenticationState authState)
    {
        var user = authState?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Vérifie si l'utilisateur connecté possède un rôle spécifique
    /// </summary>
    public static bool IsInRole(AuthenticationState authState, string role)
    {
        var user = authState?.User;
        return user?.Identity?.IsAuthenticated == true && user.IsInRole(role);
    }
}

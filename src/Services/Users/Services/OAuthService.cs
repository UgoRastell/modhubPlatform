using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using UsersService.Configuration;
using UsersService.Data;
using UsersService.Models;
using UsersService.Models.DTOs;

namespace UsersService.Services;

public interface IOAuthService
{
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken);
    Task<ExternalAuthResponse?> AuthenticateExternalUserAsync(ExternalAuthRequest request);
    Task<LinkExternalLoginResponse> LinkExternalLoginAsync(string userId, LinkExternalLoginRequest request);
    Task<bool> UnlinkExternalLoginAsync(string userId, string provider);
    Task<List<ExternalLoginInfo>> GetExternalLoginsAsync(string userId);
}

public class OAuthService : IOAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<OAuthService> _logger;
    private readonly GoogleOAuthSettings _googleSettings;

    public OAuthService(
        IHttpClientFactory httpClientFactory,
        IUserRepository userRepository,
        IAuthService authService,
        ILogger<OAuthService> logger,
        IOptions<GoogleOAuthSettings> googleSettings)
    {
        _httpClientFactory = httpClientFactory;
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
        _googleSettings = googleSettings.Value;
    }

    public async Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            // Valider le token Google avec l'API tokeninfo
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Échec de validation du token Google: {StatusCode}", response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userInfo == null)
            {
                _logger.LogWarning("Impossible de désérialiser les informations utilisateur Google");
                return null;
            }

            // Vérifier que le token est issu de notre application
            if (userInfo.Email == null || !userInfo.VerifiedEmail)
            {
                _logger.LogWarning("L'email Google n'est pas vérifié");
                return null;
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la validation du token Google");
            return null;
        }
    }

    public async Task<ExternalAuthResponse?> AuthenticateExternalUserAsync(ExternalAuthRequest request)
    {
        if (request.Provider.ToLower() != "google")
        {
            _logger.LogWarning("Provider non supporté: {Provider}", request.Provider);
            return null;
        }

        var googleUserInfo = await ValidateGoogleTokenAsync(request.IdToken);
        if (googleUserInfo == null)
        {
            return null;
        }

        // Vérifier si l'utilisateur existe déjà (par email)
        var existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email.ToLowerInvariant());

        if (existingUser != null)
        {
            // L'utilisateur existe, vérifier s'il a déjà un login Google
            var existingLogin = existingUser.ExternalLogins
                .FirstOrDefault(l => l.Provider == "google" && l.Email == googleUserInfo.Email);

            if (existingLogin == null)
            {
                // Ajouter le login Google à l'utilisateur existant
                existingUser.ExternalLogins.Add(new ExternalLogin
                {
                    Provider = "google",
                    ProviderKey = googleUserInfo.Id,
                    Email = googleUserInfo.Email,
                    DisplayName = googleUserInfo.Name,
                    PictureUrl = googleUserInfo.Picture
                });
                
                await _userRepository.UpdateAsync(existingUser.Id, existingUser);
            }
            else
            {
                // Mettre à jour les informations du login
                existingLogin.LastUsed = DateTime.UtcNow;
                existingLogin.DisplayName = googleUserInfo.Name;
                existingLogin.PictureUrl = googleUserInfo.Picture;
                
                await _userRepository.UpdateAsync(existingUser.Id, existingUser);
            }

            // Générer un token JWT pour l'utilisateur existant
            var tokenResponse = await _authService.GenerateTokenResponseForUserAsync(existingUser);
            
            return new ExternalAuthResponse
            {
                TokenResponse = tokenResponse,
                IsNewUser = false
            };
        }
        else
        {
            // Créer un nouvel utilisateur
            var newUser = new User
            {
                Username = googleUserInfo.Email.Split('@')[0] + DateTime.UtcNow.Ticks.ToString().Substring(0, 4),
                Email = googleUserInfo.Email.ToLowerInvariant(),
                FirstName = googleUserInfo.GivenName,
                LastName = googleUserInfo.FamilyName,
                ProfilePictureUrl = googleUserInfo.Picture,
                EmailVerified = true,  // Email déjà vérifié par Google
                ExternalLogins = new List<ExternalLogin>
                {
                    new ExternalLogin
                    {
                        Provider = "google",
                        ProviderKey = googleUserInfo.Id,
                        Email = googleUserInfo.Email,
                        DisplayName = googleUserInfo.Name,
                        PictureUrl = googleUserInfo.Picture
                    }
                },
                TermsAcceptedAt = DateTime.UtcNow
            };

            await _userRepository.CreateAsync(newUser);
            
            // Générer un token JWT pour le nouvel utilisateur
            var tokenResponse = await _authService.GenerateTokenResponseForUserAsync(newUser);
            
            return new ExternalAuthResponse
            {
                TokenResponse = tokenResponse,
                IsNewUser = true
            };
        }
    }

    public async Task<LinkExternalLoginResponse> LinkExternalLoginAsync(string userId, LinkExternalLoginRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new LinkExternalLoginResponse
            {
                Success = false,
                Message = "Utilisateur non trouvé"
            };
        }

        if (request.Provider.ToLower() != "google")
        {
            return new LinkExternalLoginResponse
            {
                Success = false,
                Message = "Provider non supporté"
            };
        }

        var googleUserInfo = await ValidateGoogleTokenAsync(request.IdToken);
        if (googleUserInfo == null)
        {
            return new LinkExternalLoginResponse
            {
                Success = false,
                Message = "Token d'authentification invalide"
            };
        }

        // Vérifier que le compte n'est pas déjà lié à un autre utilisateur
        var existingUser = await _userRepository.GetByEmailAsync(googleUserInfo.Email.ToLowerInvariant());
        if (existingUser != null && existingUser.Id != userId)
        {
            return new LinkExternalLoginResponse
            {
                Success = false,
                Message = "Ce compte Google est déjà associé à un autre utilisateur"
            };
        }

        // Vérifier si ce compte est déjà lié à l'utilisateur actuel
        var existingLogin = user.ExternalLogins
            .FirstOrDefault(l => l.Provider == "google" && l.ProviderKey == googleUserInfo.Id);
            
        if (existingLogin != null)
        {
            return new LinkExternalLoginResponse
            {
                Success = false,
                Message = "Ce compte Google est déjà lié à votre compte"
            };
        }

        // Ajouter le nouveau login externe
        user.ExternalLogins.Add(new ExternalLogin
        {
            Provider = "google",
            ProviderKey = googleUserInfo.Id,
            Email = googleUserInfo.Email,
            DisplayName = googleUserInfo.Name,
            PictureUrl = googleUserInfo.Picture
        });

        await _userRepository.UpdateAsync(userId, user);

        return new LinkExternalLoginResponse
        {
            Success = true,
            Message = "Compte Google lié avec succès",
            LinkedAccounts = await GetExternalLoginsAsync(userId)
        };
    }

    public async Task<bool> UnlinkExternalLoginAsync(string userId, string provider)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        // S'assurer que ce n'est pas le seul moyen de connexion
        if (string.IsNullOrEmpty(user.PasswordHash) && user.ExternalLogins.Count <= 1)
        {
            _logger.LogWarning("Tentative de suppression du seul moyen de connexion pour l'utilisateur {UserId}", userId);
            return false;
        }

        var loginToRemove = user.ExternalLogins.FirstOrDefault(l => l.Provider.ToLower() == provider.ToLower());
        if (loginToRemove != null)
        {
            user.ExternalLogins.Remove(loginToRemove);
            await _userRepository.UpdateAsync(userId, user);
            return true;
        }

        return false;
    }

    public async Task<List<ExternalLoginInfo>> GetExternalLoginsAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new List<ExternalLoginInfo>();
        }

        return user.ExternalLogins.Select(l => new ExternalLoginInfo
        {
            Provider = l.Provider,
            Email = l.Email,
            DisplayName = l.DisplayName,
            LinkedAt = l.CreatedAt
        }).ToList();
    }
}

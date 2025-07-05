using IdentityService.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace IdentityService.Services
{
    public class OAuthService : IOAuthService
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OAuthService> _logger;
        private readonly HttpClient _httpClient;

        public OAuthService(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<OAuthService> logger,
            HttpClient httpClient)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<AuthResponse> AuthenticateAsync(string provider, string token)
        {
            return provider.ToLower() switch
            {
                "google" => await AuthenticateGoogleAsync(token),
                _ => throw new UnsupportedOAuthProviderException($"Provider {provider} is not supported")
            };
        }

        private async Task<AuthResponse> AuthenticateGoogleAsync(string token)
        {
            try
            {
                var userInfo = await GetGoogleUserInfoAsync(token);
                return await AuthenticateGoogleUserAsync(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating with Google");
                throw new OAuthValidationException("Failed to authenticate with Google", ex);
            }
        }

        public async Task<TokenResponse> ExchangeGoogleAuthCodeForTokenAsync(string code)
        {
            try
            {
                var tokenEndpoint = "https://oauth2.googleapis.com/token";
                var clientId = _configuration["OAuth:Google:ClientId"] ?? 
                               _configuration["GOOGLE_CLIENT_ID"];
                var clientSecret = _configuration["OAuth:Google:ClientSecret"] ?? 
                                  _configuration["GOOGLE_CLIENT_SECRET"];
                var redirectUri = _configuration["OAuth:Google:RedirectUri"] ?? 
                                 "https://modhub.ovh/api/OAuth/google-callback";

                _logger.LogInformation("Exchanging Google code for token with client ID: {ClientIdPrefix}...", 
                    clientId?.Substring(0, Math.Min(10, clientId?.Length ?? 0)));

                var requestContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "code", code },
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "redirect_uri", redirectUri },
                    { "grant_type", "authorization_code" }
                });

                var response = await _httpClient.PostAsync(tokenEndpoint, requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Google token exchange failed: {StatusCode}, {Error}", 
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TokenResponse>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging Google auth code for token");
                throw;
            }
        }

        public async Task<GoogleUserInfo> GetGoogleUserInfoAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get Google user info: {StatusCode}, {Error}", 
                        response.StatusCode, errorContent);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<GoogleUserInfo>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Google user info");
                throw;
            }
            finally
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<AuthResponse> AuthenticateGoogleUserAsync(GoogleUserInfo userInfo)
        {
            try
            {
                // Vérifier si l'utilisateur existe déjà avec cet email
                var user = await _authService.FindUserByEmailAsync(userInfo.Email);
                var isNewUser = user == null;
                
                if (isNewUser)
                {
                    // Créer un nouvel utilisateur avec les informations Google
                    var username = GenerateUniqueUsername(userInfo.Name, userInfo.Email);
                    
                    // Générer un mot de passe aléatoire (l'utilisateur ne l'utilisera pas directement)
                    var password = GenerateRandomPassword();
                    
                    var registerResult = await _authService.RegisterUserAsync(new RegisterRequest
                    {
                        Username = username,
                        Email = userInfo.Email,
                        Password = password,
                        ConfirmPassword = password,
                        FirstName = userInfo.GivenName,
                        LastName = userInfo.FamilyName,
                        OAuthProvider = "google",
                        OAuthProviderId = userInfo.Id,
                        IsEmailVerified = userInfo.EmailVerified
                    });
                    
                    // Ajouter les informations supplémentaires au profil (photo, etc.)
                    await _authService.UpdateUserProfileAsync(registerResult.UserId, new UserProfileUpdateRequest
                    {
                        ProfilePictureUrl = userInfo.Picture
                    });
                    
                    return new AuthResponse
                    {
                        UserId = registerResult.UserId,
                        Username = username,
                        Token = registerResult.Token,
                        RefreshToken = registerResult.RefreshToken,
                        ExpiresAt = registerResult.ExpiresAt,
                        IsNewUser = true
                    };
                }
                else
                {
                    // Connecter l'utilisateur existant
                    var loginResult = await _authService.AuthenticateExternalUserAsync(userInfo.Email, "google", userInfo.Id);
                    loginResult.IsNewUser = false;
                    return loginResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating Google user");
                throw;
            }
        }

        private string GenerateUniqueUsername(string name, string email)
        {
            // Simplifier le nom pour créer un nom d'utilisateur basé sur le nom ou l'email
            var baseName = !string.IsNullOrEmpty(name)
                ? name.Replace(" ", "").ToLower()
                : email.Split('@')[0];
            
            // Ajouter un timestamp pour garantir l'unicité
            return $"{baseName}_{DateTime.UtcNow.Ticks % 10000}";
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            var password = new StringBuilder(16);

            for (int i = 0; i < 16; i++)
            {
                password.Append(chars[random.Next(chars.Length)]);
            }

            return password.ToString();
        }
    }

    // Exceptions spécifiques à OAuth
    public class UnsupportedOAuthProviderException : Exception
    {
        public UnsupportedOAuthProviderException(string message) : base(message) { }
    }

    public class OAuthValidationException : Exception
    {
        public OAuthValidationException(string message, Exception innerException = null) 
            : base(message, innerException) { }
    }
}

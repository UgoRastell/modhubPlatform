using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using UsersService.Data;
using UsersService.Models;
using UsersService.Models.DTOs;

namespace UsersService.Services;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RegisterAsync(RegisterRequest request);
    Task<TokenResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string token, string newPassword);
    Task RevokeTokenAsync(string userId);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    
    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        // Normaliser l'email
        var email = request.Email.ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email);
        
        if (user == null)
        {
            _logger.LogWarning("Tentative de connexion avec une adresse email inconnue: {Email}", email);
            return null;
        }
        
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Mot de passe incorrect pour l'utilisateur: {Email}", email);
            return null;
        }
        
        // Mettre à jour la dernière connexion
        await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);
        
        // Générer et retourner le token
        return await GenerateTokenResponseAsync(user);
    }
    
    public async Task<TokenResponse?> RegisterAsync(RegisterRequest request)
    {
        // Normaliser l'email
        var email = request.Email.ToLowerInvariant();
        
        // Vérifier que l'email n'est pas déjà utilisé
        if (await _userRepository.EmailExistsAsync(email))
        {
            _logger.LogWarning("Tentative d'inscription avec une adresse email déjà utilisée: {Email}", email);
            return null;
        }
        
        // Vérifier que le nom d'utilisateur n'est pas déjà utilisé
        if (await _userRepository.UsernameExistsAsync(request.Username))
        {
            _logger.LogWarning("Tentative d'inscription avec un nom d'utilisateur déjà utilisé: {Username}", request.Username);
            return null;
        }
        
        // Vérifier que les conditions sont acceptées
        if (!request.AcceptTerms)
        {
            _logger.LogWarning("Tentative d'inscription sans acceptation des conditions: {Email}", email);
            return null;
        }
        
        // Créer le nouvel utilisateur
        var user = new User
        {
            Username = request.Username,
            Email = email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            TermsAcceptedAt = DateTime.UtcNow
        };
        
        // Ajouter les consentements RGPD
        if (request.ConsentMarketing.HasValue)
        {
            user.DataConsents["marketing"] = new DataConsent
            {
                Consented = request.ConsentMarketing.Value,
                Timestamp = DateTime.UtcNow
            };
        }
        
        if (request.ConsentDataAnalytics.HasValue)
        {
            user.DataConsents["analytics"] = new DataConsent
            {
                Consented = request.ConsentDataAnalytics.Value,
                Timestamp = DateTime.UtcNow
            };
        }
        
        if (request.ConsentThirdParty.HasValue)
        {
            user.DataConsents["thirdParty"] = new DataConsent
            {
                Consented = request.ConsentThirdParty.Value,
                Timestamp = DateTime.UtcNow
            };
        }
        
        // Enregistrer l'utilisateur
        await _userRepository.CreateAsync(user);
        _logger.LogInformation("Nouvel utilisateur enregistré: {Email}", email);
        
        // Générer et retourner le token
        return await GenerateTokenResponseAsync(user);
    }
    
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        // À implémenter avec une gestion des refresh tokens en base de données
        // Pour le moment, on ne rafraîchit pas réellement le token
        
        // En production, on vérifierait le refreshToken dans une collection dédiée
        // et on génèrerait un nouveau token uniquement si le refreshToken est valide
        
        _logger.LogWarning("Demande de rafraîchissement de token avec fonctionnalité non implémentée");
        return null;
    }
    
    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        // Normaliser l'email
        email = email.ToLowerInvariant();
        
        // Vérifier si l'utilisateur existe
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            // Pour des raisons de sécurité, on ne dit pas si l'email existe ou non
            _logger.LogInformation("Demande de réinitialisation pour un email inexistant: {Email}", email);
            return true;
        }
        
        // Générer un token aléatoire
        var token = GenerateRandomToken();
        var expiry = DateTime.UtcNow.AddHours(24);
        
        // Enregistrer le token en base de données
        await _userRepository.SetResetTokenAsync(email, token, expiry);
        
        // En production, on enverrait un email contenant le lien de réinitialisation
        // Pour le moment, on simule l'envoi d'un email
        _logger.LogInformation("Token de réinitialisation généré pour {Email}: {Token}", email, token);
        
        return true;
    }
    
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        // Trouver l'utilisateur par le token
        var user = await _userRepository.GetByResetTokenAsync(token);
        if (user == null)
        {
            _logger.LogWarning("Tentative de réinitialisation avec un token invalide");
            return false;
        }
        
        // Mettre à jour le mot de passe
        user.PasswordHash = HashPassword(newPassword);
        
        // Enregistrer le nouvel utilisateur
        await _userRepository.UpdateAsync(user.Id, user);
        
        // Supprimer le token
        await _userRepository.ClearResetTokenAsync(user.Id);
        
        _logger.LogInformation("Mot de passe réinitialisé pour {Email}", user.Email);
        return true;
    }
    
    public async Task RevokeTokenAsync(string userId)
    {
        // À implémenter avec une gestion des tokens révoqués
        // Pour le moment, pas d'implémentation complète
        _logger.LogInformation("Demande de révocation de token pour l'utilisateur {UserId}", userId);
    }
    
    #region Méthodes privées
    
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }
    
    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    
    private string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    
    private async Task<TokenResponse> GenerateTokenResponseAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Key"] ?? 
                                        "DefaultSecureKeyWithAtLeast32Characters!");
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.Username)
        };
        
        // Ajouter les rôles comme claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                Convert.ToDouble(_configuration["JwtSettings:TokenExpirationInMinutes"] ?? "60")),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["JwtSettings:Issuer"],
            Audience = _configuration["JwtSettings:Audience"]
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        
        // Générer un refresh token (pour une implémentation complète)
        var refreshToken = GenerateRandomToken();
        
        return new TokenResponse
        {
            Token = jwtToken,
            Expires = tokenDescriptor.Expires!.Value,
            RefreshToken = refreshToken,
            User = new UserBasicInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = user.Roles,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };
    }
    
    #endregion
}

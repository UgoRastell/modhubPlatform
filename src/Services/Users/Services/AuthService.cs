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
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task RevokeTokenAsync(string userId);
    
    // Nouvelles méthodes pour la validation d'email
    Task<bool> SendEmailVerificationAsync(string email);
    Task<bool> VerifyEmailAsync(string email, string token);
    
    // Méthode pour générer un token à partir d'un utilisateur (utile pour OAuth)
    Task<TokenResponse> GenerateTokenResponseForUserAsync(User user);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService? _emailService; // Optionnel pour la rétrocompatibilité
    
    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        IEmailService? emailService = null)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
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

        // Vérifier si le compte est verrouillé (protection anti-brute-force)
        if (user.IsLocked())
        {
            _logger.LogWarning("Tentative de connexion sur un compte verrouillé: {Email}", email);
            return null;
        }
        
        if (user.PasswordHash == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            // Incrémenter le compteur d'échecs de connexion
            user.FailedLoginAttempts++;
            user.LastFailedLoginAttempt = DateTime.UtcNow;
            
            // Verrouiller le compte après un certain nombre d'échecs
            if (user.FailedLoginAttempts >= 5) // Configurable
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15); // Verrouillage temporaire
                _logger.LogWarning("Compte verrouillé après trop de tentatives: {Email}", email);
            }
            
            await _userRepository.UpdateAsync(user.Id, user);
            _logger.LogWarning("Mot de passe incorrect pour l'utilisateur: {Email}, tentative {Attempts}", 
                email, user.FailedLoginAttempts);
            return null;
        }
        
        // Réinitialiser le compteur d'échecs en cas de succès
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        
        // Mettre à jour la dernière connexion
        user.LastLogin = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user.Id, user);
        
        // Générer et retourner le token
        return await GenerateTokenResponseForUserAsync(user);
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
        
        // Initialiser la liste des rôles avec seulement 'creator'
        // (au lieu d'ajouter 'creator' en plus du rôle 'user' ajouté par défaut)
        user.Roles = new List<string> { "Creator" };
        
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
        
        // Générer un token de vérification d'email
        user.EmailVerificationToken = GenerateRandomToken();
        user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);
        
        // Enregistrer l'utilisateur
        await _userRepository.CreateAsync(user);
        _logger.LogInformation("Nouvel utilisateur enregistré: {Email}", email);
        
        // Envoyer l'email de vérification si le service d'email est disponible
        if (_emailService != null)
        {
            await _emailService.SendVerificationEmailAsync(email, user.Username, user.EmailVerificationToken);
            _logger.LogInformation("Email de vérification envoyé à: {Email}", email);
        }
        else
        {
            _logger.LogWarning("Service d'email non disponible, email de vérification non envoyé à: {Email}", email);
        }
        
        // Générer et retourner le token
        return await GenerateTokenResponseForUserAsync(user);
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
    
    // Implémentation de réinitialisation de mot de passe déplacée vers la version plus bas dans la classe
    
    // Ancienne méthode ResetPasswordAsync avec deux paramètres supprimée
    // Utiliser la méthode avec trois paramètres: ResetPasswordAsync(string email, string token, string newPassword)
    
    public async Task RevokeTokenAsync(string userId)
    {
        // À implémenter avec une gestion des tokens révoqués
        // Pour le moment, pas d'implémentation complète
        _logger.LogInformation("Demande de révocation de token pour l'utilisateur {UserId}", userId);
    }
    
    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        // Normaliser l'email
        email = email.ToLowerInvariant();
        
        // Vérifier si l'utilisateur existe
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Demande de réinitialisation pour un email inexistant: {Email}", email);
            return false;
        }
        
        // Générer un token de réinitialisation
        user.ResetToken = GenerateRandomToken();
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(24); // Token valide 24h
        
        await _userRepository.UpdateAsync(user.Id, user);
        
        // Envoyer l'email de réinitialisation si le service d'email est disponible
        if (_emailService != null)
        {
            await _emailService.SendPasswordResetEmailAsync(email, user.Username, user.ResetToken);
            _logger.LogInformation("Email de réinitialisation envoyé à: {Email}", email);
            return true;
        }
        else
        {
            // Si le service d'email n'est pas disponible, on log le token (pour le développement)
            _logger.LogInformation("Service d'email non disponible, token de réinitialisation généré pour {Email}: {Token}", email, user.ResetToken);
            return false;
        }
    }
    
    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        // Normaliser l'email
        email = email.ToLowerInvariant();
        
        // Récupérer l'utilisateur
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Tentative de réinitialisation pour un email inexistant: {Email}", email);
            return false;
        }
        
        // Vérifier que le token est valide
        if (string.IsNullOrEmpty(user.ResetToken) || user.ResetToken != token)
        {
            _logger.LogWarning("Token de réinitialisation invalide pour: {Email}", email);
            return false;
        }
        
        // Vérifier que le token n'est pas expiré
        if (!user.ResetTokenExpires.HasValue || 
            user.ResetTokenExpires.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Token de réinitialisation expiré pour: {Email}", email);
            return false;
        }
        
        // Mettre à jour le mot de passe
        user.PasswordHash = HashPassword(newPassword);
        user.ResetToken = null;
        user.ResetTokenExpires = null;
        
        // Réinitialiser également les tentatives de connexion échouées
        user.FailedLoginAttempts = 0;
        user.LastFailedLoginAttempt = null;
        user.LockedUntil = null;
        
        await _userRepository.UpdateAsync(user.Id, user);
        
        _logger.LogInformation("Mot de passe réinitialisé avec succès pour: {Email}", email);
        return true;
    }
    
    public async Task<bool> SendEmailVerificationAsync(string email)
    {
        // Normaliser l'email
        email = email.ToLowerInvariant();
        
        // Vérifier si l'utilisateur existe
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Demande de vérification pour un email inexistant: {Email}", email);
            return false;
        }
        
        // Vérifier si l'email est déjà vérifié
        if (user.EmailVerified)
        {
            _logger.LogInformation("Email déjà vérifié pour: {Email}", email);
            return true;
        }
        
        // Générer un nouveau token de vérification
        user.EmailVerificationToken = GenerateRandomToken();
        user.EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24);
        
        await _userRepository.UpdateAsync(user.Id, user);
        
        // Envoyer l'email de vérification si le service d'email est disponible
        if (_emailService != null)
        {
            await _emailService.SendVerificationEmailAsync(email, user.Username, user.EmailVerificationToken);
            _logger.LogInformation("Email de vérification envoyé à: {Email}", email);
            return true;
        }
        else
        {
            // Si le service d'email n'est pas disponible, on log le token (pour le développement)
            _logger.LogInformation("Service d'email non disponible, token de vérification généré pour {Email}: {Token}", email, user.EmailVerificationToken);
            return false;
        }
    }
    
    public async Task<bool> VerifyEmailAsync(string email, string token)
    {
        // Normaliser l'email
        email = email.ToLowerInvariant();
        
        // Récupérer l'utilisateur
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Tentative de vérification pour un email inexistant: {Email}", email);
            return false;
        }
        
        // Vérifier si l'email est déjà vérifié
        if (user.EmailVerified)
        {
            return true;
        }
        
        // Vérifier le token
        if (user.EmailVerificationToken != token)
        {
            _logger.LogWarning("Token de vérification invalide pour: {Email}", email);
            return false;
        }
        
        // Vérifier que le token n'est pas expiré
        if (!user.EmailVerificationTokenExpires.HasValue || 
            user.EmailVerificationTokenExpires.Value < DateTime.UtcNow)
        {
            _logger.LogWarning("Token de vérification expiré pour: {Email}", email);
            return false;
        }
        
        // Marquer l'email comme vérifié
        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpires = null;
        
        await _userRepository.UpdateAsync(user.Id, user);
        
        _logger.LogInformation("Email vérifié avec succès pour: {Email}", email);
        return true;
    }
    
    #region Méthodes privées
    
    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, 12);
    }
    
    private static bool VerifyPassword(string password, string? hash)
    {
        if (hash == null)
            return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    
    private string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    
    public async Task<TokenResponse> GenerateTokenResponseForUserAsync(User user)
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
        
        // Ajouter une claim pour indiquer si l'email est vérifié
        claims.Add(new Claim("email_verified", user.EmailVerified.ToString().ToLower()));
        
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
                EmailVerified = user.EmailVerified,
                Roles = user.Roles,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };
    }
    
    #endregion
}

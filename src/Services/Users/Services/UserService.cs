using System.Text.Json;
using UsersService.Data;
using UsersService.Models;
using UsersService.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace UsersService.Services;

public interface IUserService
{
    // Profil utilisateur
    Task<UserProfileResponse?> GetProfileAsync(string userId);
    Task<UserProfileResponse?> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool> UpdatePasswordAsync(string userId, UpdatePasswordRequest request);
    Task<bool> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request);
    Task<AvatarUploadResponse?> UploadAvatarAsync(string userId, IFormFile file);
    
    // Gestion RGPD
    Task<bool> UpdateConsentAsync(string userId, GdprConsentUpdateRequest request);
    Task<GdprExportResponse?> ExportUserDataAsync(string userId, string password);
    Task<bool> DeleteUserDataAsync(string userId, GdprDeleteRequest request);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<UserService> _logger;
    private readonly IConfiguration _configuration;

    public UserService(
        IUserRepository userRepository,
        IAuthService authService,
        ILogger<UserService> logger,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(string userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative d'accès à un profil inexistant: {UserId}", userId);
            return null;
        }

        return MapUserToProfileResponse(user);
    }

    public async Task<UserProfileResponse?> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative de mise à jour d'un profil inexistant: {UserId}", userId);
            return null;
        }

        // Vérifier si le nom d'utilisateur est déjà pris
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                _logger.LogWarning("Tentative de mise à jour avec un nom d'utilisateur déjà utilisé: {Username}", request.Username);
                return null;
            }
            user.Username = request.Username;
        }

        // Mise à jour des autres champs
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
            
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
            
        if (!string.IsNullOrEmpty(request.Bio))
            user.Bio = request.Bio;
            
        if (!string.IsNullOrEmpty(request.ProfilePictureUrl))
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        await _userRepository.UpdateAsync(userId, user);
        _logger.LogInformation("Profil utilisateur mis à jour: {UserId}", userId);

        return MapUserToProfileResponse(user);
    }

    public async Task<bool> UpdatePasswordAsync(string userId, UpdatePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative de changement de mot de passe pour un utilisateur inexistant: {UserId}", userId);
            return false;
        }

        // Vérifier le mot de passe actuel
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Mot de passe actuel incorrect pour l'utilisateur: {UserId}", userId);
            return false;
        }

        // Mettre à jour le mot de passe
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, 12);
        await _userRepository.UpdateAsync(userId, user);
        
        _logger.LogInformation("Mot de passe mis à jour pour l'utilisateur: {UserId}", userId);
        return true;
    }

    public async Task<bool> UpdatePreferencesAsync(string userId, UpdatePreferencesRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative de mise à jour des préférences pour un utilisateur inexistant: {UserId}", userId);
            return false;
        }

        // Mettre à jour les préférences ou les créer si elles n'existent pas
        user.Preferences ??= new UserPreferences();
        
        // Mise à jour des préférences uniquement si elles sont fournies dans la requête
        if (request.EmailNotifications.HasValue)
            user.Preferences.EmailNotifications = request.EmailNotifications.Value;
            
        if (request.MarketingEmails.HasValue)
            user.Preferences.MarketingEmails = request.MarketingEmails.Value;
        
        if (!string.IsNullOrEmpty(request.Theme))
            user.Preferences.Theme = request.Theme;
            
        if (!string.IsNullOrEmpty(request.Language))
            user.Preferences.Language = request.Language;
        
        await _userRepository.UpdateAsync(userId, user);
        _logger.LogInformation("Préférences mises à jour pour l'utilisateur: {UserId}", userId);
        
        return true;
    }

    public async Task<AvatarUploadResponse?> UploadAvatarAsync(string userId, IFormFile file)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative d'upload d'avatar pour un utilisateur inexistant: {UserId}", userId);
            return null;
        }
        
        // Générer un nom unique pour le fichier pour éviter les collisions
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var fileName = $"{userId}_{timestamp}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        
        // Chemin où les avatars sont stockés
        var uploadsDirectory = _configuration["Storage:AvatarsPath"] ?? "wwwroot/avatars";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), uploadsDirectory);
        
        // S'assurer que le répertoire existe
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
        
        var filePath = Path.Combine(fullPath, fileName);
        
        try
        {
            // Sauvegarder le fichier
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            
            // Déterminer l'URL publique de l'avatar
            var baseUrl = _configuration["BaseUrls:UsersService"] ?? "http://localhost:5002";
            var avatarRelativePath = $"avatars/{fileName}";
            var avatarUrl = $"{baseUrl}/{avatarRelativePath}";
            
            // Mettre à jour l'avatar de l'utilisateur dans la base de données
            user.ProfilePictureUrl = avatarUrl;
            await _userRepository.UpdateAsync(userId, user);
            
            _logger.LogInformation("Avatar mis à jour pour l'utilisateur: {UserId}, URL: {AvatarUrl}", userId, avatarUrl);
            
            return new AvatarUploadResponse
            {
                AvatarUrl = avatarUrl,
                Message = "Avatar mis à jour avec succès",
                UploadedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'upload d'avatar pour l'utilisateur {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateConsentAsync(string userId, GdprConsentUpdateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative de mise à jour du consentement pour un utilisateur inexistant: {UserId}", userId);
            return false;
        }

        // Mettre à jour ou créer le consentement
        user.DataConsents[request.ConsentKey] = new DataConsent
        {
            Consented = request.Consented,
            Timestamp = DateTime.UtcNow
        };

        await _userRepository.UpdateAsync(userId, user);
        _logger.LogInformation("Consentement {ConsentKey} mis à jour pour l'utilisateur {UserId}: {Consented}", 
            request.ConsentKey, userId, request.Consented);
            
        return true;
    }

    public async Task<GdprExportResponse?> ExportUserDataAsync(string userId, string password)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative d'exportation de données pour un utilisateur inexistant: {UserId}", userId);
            return null;
        }

        // Vérifier le mot de passe pour sécuriser l'export
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.LogWarning("Mot de passe incorrect lors de la tentative d'exportation RGPD: {UserId}", userId);
            return null;
        }

        // En production, on récupérerait les activités de l'utilisateur depuis une collection dédiée
        // Pour le moment, on retourne une liste vide d'activités
        var activities = new List<UserActivity>();

        _logger.LogInformation("Exportation des données pour l'utilisateur: {UserId}", userId);
        
        return new GdprExportResponse
        {
            Profile = MapUserToProfileResponse(user),
            Consents = user.DataConsents,
            Activities = activities,
            ExportDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteUserDataAsync(string userId, GdprDeleteRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Tentative de suppression d'un utilisateur inexistant: {UserId}", userId);
            return false;
        }

        // Vérifier le mot de passe et la confirmation
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash) || !request.ConfirmDeletion)
        {
            _logger.LogWarning("Authentification incorrecte ou confirmation manquante lors de la suppression: {UserId}", userId);
            return false;
        }

        // En production, on enregistrerait la raison de la suppression et on anonymiserait les données
        // Pour des raisons de simplicité, on supprime complètement l'utilisateur ici
        
        await _userRepository.DeleteAsync(userId);
        
        // Révoquer tous les tokens de l'utilisateur
        await _authService.RevokeTokenAsync(userId);
        
        _logger.LogInformation("Utilisateur supprimé: {UserId}, Raison: {Reason}", userId, request.Reason);
        return true;
    }

    #region Méthodes privées

    private static UserProfileResponse MapUserToProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio,
            Roles = user.Roles,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Preferences = user.Preferences
        };
    }

    #endregion
}

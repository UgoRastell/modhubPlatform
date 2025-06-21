using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace UsersService.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("username")]
    public string Username { get; set; } = null!;

    [BsonElement("email")]
    public string Email { get; set; } = null!;

    [BsonElement("passwordHash")]
    [JsonIgnore] // Ne jamais envoyer le hash de mot de passe au client
    public string? PasswordHash { get; set; } // Nullable pour les auth externes (OAuth)

    [BsonElement("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("profilePictureUrl")]
    public string? ProfilePictureUrl { get; set; }

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("roles")]
    public List<string> Roles { get; set; } = new List<string> { "User" };

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLogin")]
    public DateTime? LastLogin { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    // Champs pour OAuth
    [BsonElement("externalLogins")]
    public List<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    // Validation d'email
    [BsonElement("emailVerified")]
    public bool EmailVerified { get; set; } = false;
    
    [BsonElement("verificationToken")]
    [JsonIgnore]
    public string? EmailVerificationToken { get; set; }
    
    [BsonElement("verificationTokenExpires")]
    [JsonIgnore]
    public DateTime? EmailVerificationTokenExpires { get; set; }
    
    // Nombre de tentatives de connexion échouées pour protection anti-brute-force
    [BsonElement("failedLoginAttempts")]
    public int FailedLoginAttempts { get; set; } = 0;
    
    [BsonElement("lastFailedLoginAttempt")]
    public DateTime? LastFailedLoginAttempt { get; set; }
    
    [BsonElement("lockedUntil")]
    public DateTime? LockedUntil { get; set; }

    // Préférences utilisateur pour les notifications, etc.
    [BsonElement("preferences")]
    public UserPreferences Preferences { get; set; } = new UserPreferences();

    // Dernière mise à jour des conditions d'utilisation (pour RGPD)
    [BsonElement("termsAcceptedAt")]
    public DateTime? TermsAcceptedAt { get; set; }

    // Consentements RGPD spécifiques
    [BsonElement("dataConsents")]
    public Dictionary<string, DataConsent> DataConsents { get; set; } = new Dictionary<string, DataConsent>();

    // Pour les tokens de réinitialisation de mot de passe
    [BsonElement("resetToken")]
    [JsonIgnore]
    public string? ResetToken { get; set; }

    [BsonElement("resetTokenExpires")]
    [JsonIgnore]
    public DateTime? ResetTokenExpires { get; set; }
    
    // Méthode pour vérifier si le compte est verrouillé
    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;
}

public class UserPreferences
{
    [BsonElement("emailNotifications")]
    public bool EmailNotifications { get; set; } = true;

    [BsonElement("marketingEmails")]
    public bool MarketingEmails { get; set; } = false;

    [BsonElement("theme")]
    public string Theme { get; set; } = "light";

    [BsonElement("language")]
    public string Language { get; set; } = "fr";
}

public class DataConsent
{
    [BsonElement("consented")]
    public bool Consented { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("ipAddress")]
    public string? IpAddress { get; set; }
}

public class ExternalLogin
{
    [BsonElement("provider")]
    public string Provider { get; set; } = null!; // google, facebook, etc.
    
    [BsonElement("providerKey")]
    public string ProviderKey { get; set; } = null!; // ID unique fourni par le provider
    
    [BsonElement("email")]
    public string Email { get; set; } = null!;
    
    [BsonElement("displayName")]
    public string? DisplayName { get; set; }
    
    [BsonElement("pictureUrl")]
    public string? PictureUrl { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [BsonElement("lastUsed")]
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
}

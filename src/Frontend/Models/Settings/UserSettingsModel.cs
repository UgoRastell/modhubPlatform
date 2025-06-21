using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Frontend.Models.Settings
{
    /// <summary>
    /// Modèle principal contenant tous les paramètres utilisateur
    /// </summary>
    public class UserSettingsModel
    {
        public ProfileSettings Profile { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public NotificationSettings Notifications { get; set; } = new();
        public PrivacySettings Privacy { get; set; } = new();
        public PaymentSettings Payment { get; set; } = new();
        public DisplaySettings Display { get; set; } = new();
        public IntegrationSettings Integrations { get; set; } = new();
        public DangerZoneSettings DangerZone { get; set; } = new();
    }

    /// <summary>
    /// Paramètres du profil utilisateur
    /// </summary>
    public class ProfileSettings
    {
        [Required(ErrorMessage = "Le pseudo est obligatoire")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "Le pseudo doit contenir entre 3 et 32 caractères")]
        public string Username { get; set; } = string.Empty;
        
        [StringLength(256, ErrorMessage = "La bio ne peut pas dépasser 256 caractères")]
        public string Biography { get; set; } = string.Empty;
        
        public string AvatarUrl { get; set; } = string.Empty;
        
        public List<SocialLink> SocialLinks { get; set; } = new();
        
        // Statistiques en lecture seule
        public int TotalMods { get; set; }
        public int TotalDownloads { get; set; }
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// Lien vers un réseau social
    /// </summary>
    public class SocialLink
    {
        [Required(ErrorMessage = "Le type de réseau social est obligatoire")]
        public SocialPlatform Platform { get; set; }
        
        [Required(ErrorMessage = "L'identifiant ou le lien est obligatoire")]
        [Url(ErrorMessage = "Le format de l'URL n'est pas valide")]
        public string Url { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Le texte d'affichage ne peut pas dépasser 50 caractères")]
        public string DisplayText { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types de plateformes sociales supportées
    /// </summary>
    public enum SocialPlatform
    {
        Discord,
        Twitch,
        GitHub,
        Twitter,
        YouTube,
        Instagram,
        TikTok,
        Reddit,
        Other
    }

    /// <summary>
    /// Paramètres de sécurité
    /// </summary>
    public class SecuritySettings
    {
        [EmailAddress(ErrorMessage = "Le format de l'adresse e-mail n'est pas valide")]
        public string Email { get; set; } = string.Empty;
        
        public bool TwoFactorEnabled { get; set; }
        
        public List<ActiveSession> ActiveSessions { get; set; } = new();
        
        public List<ApiKey> ApiKeys { get; set; } = new();
    }

    /// <summary>
    /// Représente une session active de l'utilisateur
    /// </summary>
    public class ActiveSession
    {
        public string DeviceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool IsCurrentSession { get; set; }
    }

    /// <summary>
    /// Clé API générée par l'utilisateur
    /// </summary>
    public class ApiKey
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public List<string> Scopes { get; set; } = new();
        public string LastUsed { get; set; } = string.Empty;
    }

    /// <summary>
    /// Paramètres de notifications
    /// </summary>
    public class NotificationSettings
    {
        public bool InAppNotificationsEnabled { get; set; } = true;
        public bool EmailNotificationsEnabled { get; set; } = true;
        public bool PushNotificationsEnabled { get; set; } = false;
        
        public NotificationPreferences Preferences { get; set; } = new();
        public EmailDigestFrequency EmailDigestFrequency { get; set; } = EmailDigestFrequency.Daily;
    }

    /// <summary>
    /// Préférences détaillées pour les notifications
    /// </summary>
    public class NotificationPreferences
    {
        public bool NewComments { get; set; } = true;
        public bool ModUpdates { get; set; } = true;
        public bool Sales { get; set; } = true;
        public bool Mentions { get; set; } = true;
        public bool NewFollowers { get; set; } = true;
        public bool SystemAnnouncements { get; set; } = true;
    }

    /// <summary>
    /// Fréquence des résumés par e-mail
    /// </summary>
    public enum EmailDigestFrequency
    {
        Disabled,
        Daily,
        Weekly
    }

    /// <summary>
    /// Paramètres de confidentialité et RGPD
    /// </summary>
    public class PrivacySettings
    {
        public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.Public;
        public bool HideStatistics { get; set; } = false;
        
        public CookiePreferences CookiePreferences { get; set; } = new();
    }

    /// <summary>
    /// Options de visibilité du profil
    /// </summary>
    public enum ProfileVisibility
    {
        Public,
        FollowersOnly,
        Private
    }

    /// <summary>
    /// Préférences de cookies selon le RGPD
    /// </summary>
    public class CookiePreferences
    {
        public bool EssentialCookies { get; set; } = true; // Toujours vrai, non modifiable
        public bool AnalyticsCookies { get; set; } = true;
        public bool MarketingCookies { get; set; } = false;
        public bool PersonalizationCookies { get; set; } = true;
    }

    /// <summary>
    /// Paramètres de paiement et facturation
    /// </summary>
    public class PaymentSettings
    {
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
        public List<Transaction> TransactionHistory { get; set; } = new();
        
        public CreatorRevenue CreatorRevenue { get; set; } = new();
        public TaxSettings TaxSettings { get; set; } = new();
    }

    /// <summary>
    /// Méthode de paiement (carte, PayPal, etc.)
    /// </summary>
    public class PaymentMethod
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "card", "paypal", etc.
        public string DisplayName { get; set; } = string.Empty; // "Visa •••• 1234"
        public bool IsDefault { get; set; }
        public DateTime ExpiryDate { get; set; } // Pour les cartes
    }

    /// <summary>
    /// Transaction d'achat ou de vente
    /// </summary>
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "completed", "pending", "refunded"
        public string ReceiptUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Revenus en tant que créateur de contenu
    /// </summary>
    public class CreatorRevenue
    {
        public decimal CurrentBalance { get; set; }
        public decimal PayoutThreshold { get; set; }
        public DateTime NextPayoutDate { get; set; }
        public List<Payout> PayoutHistory { get; set; } = new();
    }

    /// <summary>
    /// Versement de revenus
    /// </summary>
    public class Payout
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
    }

    /// <summary>
    /// Informations fiscales
    /// </summary>
    public class TaxSettings
    {
        public string Country { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty; // Numéro TVA, etc.
        public bool TaxDocumentsSubmitted { get; set; } = false;
        public string BusinessType { get; set; } = string.Empty; // "individual", "company", etc.
    }

    /// <summary>
    /// Préférences d'affichage
    /// </summary>
    public class DisplaySettings
    {
        public string Theme { get; set; } = "dark";
        public string Language { get; set; } = "fr";
        public string Layout { get; set; } = "grid";
        public bool AutoPlayVideos { get; set; } = false;
    }

    /// <summary>
    /// Intégrations avec services externes
    /// </summary>
    public class IntegrationSettings
    {
        public List<ExternalIntegration> ConnectedServices { get; set; } = new();
    }

    /// <summary>
    /// Service externe connecté
    /// </summary>
    public class ExternalIntegration
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime ConnectedSince { get; set; }
        public string Username { get; set; } = string.Empty;
        public List<string> Scopes { get; set; } = new();
    }

    /// <summary>
    /// Paramètres de la zone sensible (danger zone)
    /// </summary>
    public class DangerZoneSettings
    {
        public bool DisableAllMods { get; set; } = false;
        public DateTime AccountDeletionRequested { get; set; }
    }
}

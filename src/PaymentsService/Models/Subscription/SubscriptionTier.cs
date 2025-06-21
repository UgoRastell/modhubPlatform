using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace PaymentsService.Models.Subscription
{
    /// <summary>
    /// Niveau d'abonnement avec différentes fonctionnalités et prix
    /// </summary>
    public class SubscriptionTier
    {
        /// <summary>
        /// Identifiant unique du niveau d'abonnement
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Identifiant externe (Stripe ou autre système de paiement)
        /// </summary>
        public string ExternalId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du niveau d'abonnement (ex: Basique, Premium, Pro)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description du niveau d'abonnement
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Prix mensuel
        /// </summary>
        public decimal MonthlyPrice { get; set; }
        
        /// <summary>
        /// Prix annuel (généralement avec une réduction)
        /// </summary>
        public decimal YearlyPrice { get; set; }
        
        /// <summary>
        /// Devise (EUR, USD, etc.)
        /// </summary>
        public string Currency { get; set; } = "EUR";
        
        /// <summary>
        /// Liste des avantages inclus dans ce niveau
        /// </summary>
        public List<SubscriptionBenefit> Benefits { get; set; } = new List<SubscriptionBenefit>();
        
        /// <summary>
        /// Si ce niveau est recommandé (mis en avant dans l'UI)
        /// </summary>
        public bool IsRecommended { get; set; } = false;
        
        /// <summary>
        /// Si ce niveau est actif et peut être souscrit
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Ordre d'affichage dans la liste des abonnements
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
        
        /// <summary>
        /// URL de l'image ou icône représentative
        /// </summary>
        public string ImageUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Couleur principale associée à ce niveau (pour l'UI)
        /// </summary>
        public string ThemeColor { get; set; } = "#3498db";
        
        /// <summary>
        /// Période d'essai en jours (0 = pas d'essai)
        /// </summary>
        public int TrialPeriodDays { get; set; } = 0;
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière mise à jour
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Représente un avantage spécifique inclus dans un niveau d'abonnement
    /// </summary>
    public class SubscriptionBenefit
    {
        /// <summary>
        /// Nom de l'avantage
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description détaillée de l'avantage
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Icône associée à l'avantage (ex: "download", "cloud", etc.)
        /// </summary>
        public string Icon { get; set; } = string.Empty;
        
        /// <summary>
        /// Catégorie de l'avantage (ex: Téléchargements, Support, etc.)
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Valeur numérique associée (ex: 5 téléchargements, 10 GB stockage)
        /// </summary>
        public int? NumericValue { get; set; }
        
        /// <summary>
        /// Unité de la valeur numérique (ex: GB, téléchargements, etc.)
        /// </summary>
        public string? ValueUnit { get; set; }
        
        /// <summary>
        /// Si cet avantage est mis en évidence
        /// </summary>
        public bool IsHighlighted { get; set; } = false;
    }
    
    /// <summary>
    /// Historique des modifications de niveau d'abonnement
    /// </summary>
    public class SubscriptionTierHistory
    {
        /// <summary>
        /// Identifiant unique de l'entrée d'historique
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du niveau d'abonnement
        /// </summary>
        public string SubscriptionTierId { get; set; } = string.Empty;
        
        /// <summary>
        /// Prix mensuel précédent
        /// </summary>
        public decimal? PreviousMonthlyPrice { get; set; }
        
        /// <summary>
        /// Prix annuel précédent
        /// </summary>
        public decimal? PreviousYearlyPrice { get; set; }
        
        /// <summary>
        /// Date de la modification
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Utilisateur ayant effectué la modification
        /// </summary>
        public string ModifiedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Note explicative sur la modification
        /// </summary>
        public string ChangeNotes { get; set; } = string.Empty;
    }
}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Gamification
{
    /// <summary>
    /// Badge ou récompense pouvant être attribué à un utilisateur
    /// </summary>
    public class Badge
    {
        /// <summary>
        /// Identifiant unique du badge
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom du badge
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description du badge
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de l'icône du badge
        /// </summary>
        public string IconUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Image de fond pour le badge
        /// </summary>
        public string? BackgroundImageUrl { get; set; }
        
        /// <summary>
        /// Catégorie du badge
        /// </summary>
        public string Category { get; set; } = "General";
        
        /// <summary>
        /// Niveau du badge (bronze, argent, or, etc.)
        /// </summary>
        public BadgeLevel Level { get; set; } = BadgeLevel.Bronze;
        
        /// <summary>
        /// Points de réputation accordés lorsqu'un utilisateur obtient ce badge
        /// </summary>
        public int ReputationPoints { get; set; } = 0;
        
        /// <summary>
        /// Si ce badge est secret (non visible jusqu'à ce qu'il soit obtenu)
        /// </summary>
        public bool IsSecret { get; set; } = false;
        
        /// <summary>
        /// Si ce badge est limité dans le temps (événements, etc.)
        /// </summary>
        public bool IsLimited { get; set; } = false;
        
        /// <summary>
        /// Date de début de disponibilité
        /// </summary>
        public DateTime? AvailableFrom { get; set; }
        
        /// <summary>
        /// Date de fin de disponibilité
        /// </summary>
        public DateTime? AvailableTo { get; set; }
        
        /// <summary>
        /// Type de badge (automatique, manuel, progression)
        /// </summary>
        public BadgeType Type { get; set; } = BadgeType.Automatic;
        
        /// <summary>
        /// Conditions d'obtention du badge
        /// </summary>
        public List<BadgeCondition>? Conditions { get; set; }
        
        /// <summary>
        /// Si ce badge fait partie d'une série (ensemble de badges liés)
        /// </summary>
        public string? BadgeSeriesId { get; set; }
        
        /// <summary>
        /// Position dans la série
        /// </summary>
        public int? PositionInSeries { get; set; }
        
        /// <summary>
        /// Date de création du badge
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Niveau de prestige d'un badge
    /// </summary>
    public enum BadgeLevel
    {
        /// <summary>
        /// Badge de niveau bronze (débutant)
        /// </summary>
        Bronze,
        
        /// <summary>
        /// Badge de niveau argent (intermédiaire)
        /// </summary>
        Silver,
        
        /// <summary>
        /// Badge de niveau or (avancé)
        /// </summary>
        Gold,
        
        /// <summary>
        /// Badge de niveau platine (expert)
        /// </summary>
        Platinum,
        
        /// <summary>
        /// Badge spécial (événements, promotions)
        /// </summary>
        Special
    }
    
    /// <summary>
    /// Type d'attribution du badge
    /// </summary>
    public enum BadgeType
    {
        /// <summary>
        /// Badge attribué automatiquement lorsque les conditions sont remplies
        /// </summary>
        Automatic,
        
        /// <summary>
        /// Badge attribué manuellement par un administrateur
        /// </summary>
        Manual,
        
        /// <summary>
        /// Badge de progression (plusieurs niveaux)
        /// </summary>
        Progressive
    }
    
    /// <summary>
    /// Condition pour obtenir un badge
    /// </summary>
    public class BadgeCondition
    {
        /// <summary>
        /// Type de condition (nombre de posts, ancienneté, etc.)
        /// </summary>
        public string ConditionType { get; set; } = string.Empty;
        
        /// <summary>
        /// Valeur requise pour la condition
        /// </summary>
        public int RequiredValue { get; set; } = 0;
        
        /// <summary>
        /// Opérateur de comparaison (égal, supérieur, etc.)
        /// </summary>
        public string Operator { get; set; } = "GreaterThanOrEqual";
        
        /// <summary>
        /// Champ supplémentaire pour spécifier plus de détails sur la condition
        /// </summary>
        public string? Parameter { get; set; }
    }
}

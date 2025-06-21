using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityService.Models.Gamification;

namespace CommunityService.Services.Gamification
{
    /// <summary>
    /// Interface pour le service de gestion des badges et récompenses
    /// </summary>
    public interface IBadgeService
    {
        /// <summary>
        /// Récupère tous les badges disponibles
        /// </summary>
        Task<List<Badge>> GetAllBadgesAsync(bool includeSecret = false);
        
        /// <summary>
        /// Récupère un badge par son ID
        /// </summary>
        Task<Badge> GetBadgeByIdAsync(string badgeId);
        
        /// <summary>
        /// Récupère les badges par catégorie
        /// </summary>
        Task<List<Badge>> GetBadgesByCategoryAsync(string category);
        
        /// <summary>
        /// Récupère les badges par niveau
        /// </summary>
        Task<List<Badge>> GetBadgesByLevelAsync(BadgeLevel level);
        
        /// <summary>
        /// Récupère les badges d'une série
        /// </summary>
        Task<List<Badge>> GetBadgesBySeriesAsync(string seriesId);
        
        /// <summary>
        /// Récupère les badges obtenus par un utilisateur
        /// </summary>
        Task<List<UserBadge>> GetUserBadgesAsync(string userId);
        
        /// <summary>
        /// Crée un nouveau badge
        /// </summary>
        Task<Badge> CreateBadgeAsync(Badge badge);
        
        /// <summary>
        /// Met à jour un badge existant
        /// </summary>
        Task<bool> UpdateBadgeAsync(string badgeId, Badge badge);
        
        /// <summary>
        /// Supprime un badge
        /// </summary>
        Task<bool> DeleteBadgeAsync(string badgeId);
        
        /// <summary>
        /// Attribue un badge à un utilisateur
        /// </summary>
        Task<UserBadge> AwardBadgeToUserAsync(string badgeId, string userId, string reason = null);
        
        /// <summary>
        /// Révoque un badge d'un utilisateur
        /// </summary>
        Task<bool> RevokeBadgeFromUserAsync(string badgeId, string userId, string reason = null);
        
        /// <summary>
        /// Vérifie les conditions d'attribution de badges pour un utilisateur
        /// </summary>
        Task<List<Badge>> CheckEligibleBadgesForUserAsync(string userId);
        
        /// <summary>
        /// Récupère les badges récemment attribués tous utilisateurs confondus
        /// </summary>
        Task<List<UserBadge>> GetRecentlyAwardedBadgesAsync(int count = 10);
        
        /// <summary>
        /// Récupère les badges les plus rares (moins souvent attribués)
        /// </summary>
        Task<List<BadgeRarityInfo>> GetRarestBadgesAsync(int count = 5);
        
        /// <summary>
        /// Récupère les badges les plus répandus (souvent attribués)
        /// </summary>
        Task<List<BadgeRarityInfo>> GetMostCommonBadgesAsync(int count = 5);
        
        /// <summary>
        /// Récupère les badges actifs pour une période donnée (événements)
        /// </summary>
        Task<List<Badge>> GetActiveBadgesAsync(DateTime date);
        
        /// <summary>
        /// Met à jour les points de réputation d'un utilisateur
        /// </summary>
        Task<int> UpdateUserReputationAsync(string userId, int pointsToAdd, string reason);
        
        /// <summary>
        /// Récupère les utilisateurs avec le plus de points de réputation
        /// </summary>
        Task<List<UserReputationInfo>> GetTopUsersByReputationAsync(int count = 10);
    }
    
    /// <summary>
    /// Badge attribué à un utilisateur
    /// </summary>
    public class UserBadge
    {
        /// <summary>
        /// ID unique de l'attribution
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur (pour affichage)
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// ID du badge attribué
        /// </summary>
        public string BadgeId { get; set; } = string.Empty;
        
        /// <summary>
        /// Données du badge (pour éviter de rechercher le badge séparément)
        /// </summary>
        public Badge BadgeData { get; set; } = null!;
        
        /// <summary>
        /// Date d'obtention du badge
        /// </summary>
        public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Raison de l'attribution
        /// </summary>
        public string? AwardReason { get; set; }
        
        /// <summary>
        /// Si c'est une attribution manuelle par l'équipe
        /// </summary>
        public bool IsManuallyAwarded { get; set; } = false;
    }
    
    /// <summary>
    /// Informations sur la rareté d'un badge
    /// </summary>
    public class BadgeRarityInfo
    {
        /// <summary>
        /// Données du badge
        /// </summary>
        public Badge Badge { get; set; } = null!;
        
        /// <summary>
        /// Nombre d'utilisateurs ayant obtenu ce badge
        /// </summary>
        public int AwardCount { get; set; }
        
        /// <summary>
        /// Pourcentage d'utilisateurs ayant ce badge
        /// </summary>
        public float Percentage { get; set; }
    }
    
    /// <summary>
    /// Informations sur la réputation d'un utilisateur
    /// </summary>
    public class UserReputationInfo
    {
        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur
        /// </summary>
        public string Username { get; set; } = string.Empty;
        
        /// <summary>
        /// Points de réputation totaux
        /// </summary>
        public int ReputationPoints { get; set; }
        
        /// <summary>
        /// Nombre de badges obtenus
        /// </summary>
        public int BadgeCount { get; set; }
        
        /// <summary>
        /// Liste des badges obtenus
        /// </summary>
        public List<Badge>? TopBadges { get; set; }
    }
}

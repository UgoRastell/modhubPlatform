using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityService.Models.Ratings;

namespace CommunityService.Services.Ratings
{
    /// <summary>
    /// Interface pour le service de gestion des avis et évaluations
    /// </summary>
    public interface IReviewService
    {
        /// <summary>
        /// Récupère tous les avis pour un mod spécifique
        /// </summary>
        Task<List<ModReview>> GetReviewsByModIdAsync(string modId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère un avis par son ID
        /// </summary>
        Task<ModReview> GetReviewByIdAsync(string reviewId);
        
        /// <summary>
        /// Récupère les avis créés par un utilisateur spécifique
        /// </summary>
        Task<List<ModReview>> GetReviewsByUserIdAsync(string userId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère les avis pour une version spécifique d'un mod
        /// </summary>
        Task<List<ModReview>> GetReviewsByModVersionAsync(string modId, string version, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère les avis les mieux notés pour un mod
        /// </summary>
        Task<List<ModReview>> GetTopRatedReviewsAsync(string modId, int count = 5);
        
        /// <summary>
        /// Récupère les avis les plus utiles pour un mod
        /// </summary>
        Task<List<ModReview>> GetMostHelpfulReviewsAsync(string modId, int count = 5);
        
        /// <summary>
        /// Crée un nouvel avis
        /// </summary>
        Task<ModReview> CreateReviewAsync(ModReview review);
        
        /// <summary>
        /// Met à jour un avis existant
        /// </summary>
        Task<bool> UpdateReviewAsync(string reviewId, ModReview review);
        
        /// <summary>
        /// Supprime un avis
        /// </summary>
        Task<bool> DeleteReviewAsync(string reviewId);
        
        /// <summary>
        /// Vote pour indiquer qu'un avis est utile
        /// </summary>
        Task<bool> VoteReviewHelpfulAsync(string reviewId, string userId);
        
        /// <summary>
        /// Vote pour indiquer qu'un avis n'est pas utile
        /// </summary>
        Task<bool> VoteReviewUnhelpfulAsync(string reviewId, string userId);
        
        /// <summary>
        /// Annule un vote sur un avis
        /// </summary>
        Task<bool> RemoveReviewVoteAsync(string reviewId, string userId);
        
        /// <summary>
        /// Ajoute un commentaire à un avis
        /// </summary>
        Task<ReviewComment> AddCommentToReviewAsync(string reviewId, ReviewComment comment);
        
        /// <summary>
        /// Supprime un commentaire d'un avis
        /// </summary>
        Task<bool> DeleteCommentFromReviewAsync(string reviewId, string commentId);
        
        /// <summary>
        /// Signale un avis pour modération
        /// </summary>
        Task<bool> FlagReviewAsync(string reviewId, string userId, string reason);
        
        /// <summary>
        /// Approuve un avis signalé
        /// </summary>
        Task<bool> ApproveReviewAsync(string reviewId);
        
        /// <summary>
        /// Récupère les statistiques d'évaluation pour un mod
        /// </summary>
        Task<ReviewStatistics> GetModReviewStatisticsAsync(string modId);
    }
    
    /// <summary>
    /// Statistiques d'évaluation pour un mod
    /// </summary>
    public class ReviewStatistics
    {
        /// <summary>
        /// Note moyenne sur 5
        /// </summary>
        public float AverageRating { get; set; }
        
        /// <summary>
        /// Nombre total d'avis
        /// </summary>
        public int TotalReviews { get; set; }
        
        /// <summary>
        /// Répartition des notes par étoile (1 à 5)
        /// </summary>
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
        
        /// <summary>
        /// Pourcentage d'avis positifs (4-5 étoiles)
        /// </summary>
        public float PositivePercentage { get; set; }
        
        /// <summary>
        /// Tags les plus fréquemment utilisés dans les avis
        /// </summary>
        public List<TagFrequency> TopTags { get; set; } = new List<TagFrequency>();
        
        /// <summary>
        /// Temps d'utilisation moyen rapporté dans les avis (en heures)
        /// </summary>
        public float? AverageUsageTime { get; set; }
    }
    
    /// <summary>
    /// Fréquence d'un tag dans les avis
    /// </summary>
    public class TagFrequency
    {
        /// <summary>
        /// Nom du tag
        /// </summary>
        public string Tag { get; set; } = string.Empty;
        
        /// <summary>
        /// Nombre d'occurrences
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// Pourcentage par rapport au total des avis
        /// </summary>
        public float Percentage { get; set; }
    }
}

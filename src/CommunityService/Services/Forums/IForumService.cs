using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityService.Models.Forum;

namespace CommunityService.Services.Forums
{
    /// <summary>
    /// Interface pour le service de gestion du forum
    /// </summary>
    public interface IForumService
    {
        #region Catégories
        /// <summary>
        /// Récupère toutes les catégories de forum
        /// </summary>
        Task<List<ForumCategory>> GetAllCategoriesAsync();
        
        /// <summary>
        /// Récupère les catégories de premier niveau
        /// </summary>
        Task<List<ForumCategory>> GetRootCategoriesAsync();
        
        /// <summary>
        /// Récupère une catégorie par son ID
        /// </summary>
        Task<ForumCategory> GetCategoryByIdAsync(string categoryId);
        
        /// <summary>
        /// Récupère les sous-catégories d'une catégorie
        /// </summary>
        Task<List<ForumCategory>> GetSubcategoriesAsync(string parentCategoryId);
        
        /// <summary>
        /// Crée une nouvelle catégorie
        /// </summary>
        Task<ForumCategory> CreateCategoryAsync(ForumCategory category);
        
        /// <summary>
        /// Met à jour une catégorie existante
        /// </summary>
        Task<bool> UpdateCategoryAsync(string categoryId, ForumCategory category);
        
        /// <summary>
        /// Supprime une catégorie
        /// </summary>
        Task<bool> DeleteCategoryAsync(string categoryId);
        #endregion
        
        #region Sujets
        /// <summary>
        /// Récupère les sujets d'une catégorie
        /// </summary>
        Task<List<ForumTopic>> GetTopicsByCategoryAsync(string categoryId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère un sujet par son ID
        /// </summary>
        Task<ForumTopic> GetTopicByIdAsync(string topicId);
        
        /// <summary>
        /// Recherche des sujets par mot-clé
        /// </summary>
        Task<List<ForumTopic>> SearchTopicsAsync(string query, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Crée un nouveau sujet
        /// </summary>
        Task<ForumTopic> CreateTopicAsync(ForumTopic topic, string initialPostContent);
        
        /// <summary>
        /// Met à jour un sujet existant
        /// </summary>
        Task<bool> UpdateTopicAsync(string topicId, ForumTopic topic);
        
        /// <summary>
        /// Supprime un sujet
        /// </summary>
        Task<bool> DeleteTopicAsync(string topicId);
        
        /// <summary>
        /// Épingle un sujet
        /// </summary>
        Task<bool> PinTopicAsync(string topicId, bool isPinned);
        
        /// <summary>
        /// Verrouille un sujet
        /// </summary>
        Task<bool> LockTopicAsync(string topicId, bool isLocked);
        
        /// <summary>
        /// Marque un sujet comme résolu
        /// </summary>
        Task<bool> MarkTopicAsSolvedAsync(string topicId, string solutionPostId);
        #endregion
        
        #region Messages
        /// <summary>
        /// Récupère les messages d'un sujet
        /// </summary>
        Task<List<ForumPost>> GetPostsByTopicAsync(string topicId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère un message par son ID
        /// </summary>
        Task<ForumPost> GetPostByIdAsync(string postId);
        
        /// <summary>
        /// Crée un nouveau message
        /// </summary>
        Task<ForumPost> CreatePostAsync(ForumPost post);
        
        /// <summary>
        /// Met à jour un message existant
        /// </summary>
        Task<bool> UpdatePostAsync(string postId, string newContent, string editReason = null);
        
        /// <summary>
        /// Supprime un message
        /// </summary>
        Task<bool> DeletePostAsync(string postId);
        
        /// <summary>
        /// Ajoute une réaction à un message
        /// </summary>
        Task<bool> AddReactionAsync(string postId, string userId, string reactionType);
        
        /// <summary>
        /// Supprime une réaction d'un message
        /// </summary>
        Task<bool> RemoveReactionAsync(string postId, string userId, string reactionType);
        
        /// <summary>
        /// Signale un message pour modération
        /// </summary>
        Task<bool> FlagPostAsync(string postId, string userId, string reason);
        #endregion
        
        #region Statistiques
        /// <summary>
        /// Récupère les statistiques générales du forum
        /// </summary>
        Task<ForumStatistics> GetForumStatisticsAsync();
        
        /// <summary>
        /// Récupère les sujets récemment actifs
        /// </summary>
        Task<List<ForumTopic>> GetRecentlyActiveTopicsAsync(int count = 5);
        
        /// <summary>
        /// Récupère les sujets populaires
        /// </summary>
        Task<List<ForumTopic>> GetPopularTopicsAsync(int count = 5);
        #endregion
    }
    
    /// <summary>
    /// Statistiques générales du forum
    /// </summary>
    public class ForumStatistics
    {
        /// <summary>
        /// Nombre total de catégories
        /// </summary>
        public int TotalCategories { get; set; }
        
        /// <summary>
        /// Nombre total de sujets
        /// </summary>
        public int TotalTopics { get; set; }
        
        /// <summary>
        /// Nombre total de messages
        /// </summary>
        public int TotalPosts { get; set; }
        
        /// <summary>
        /// Nombre de membres actifs sur le forum
        /// </summary>
        public int ActiveMembers { get; set; }
        
        // ---- Legacy properties expected by older service code ----
        /// <summary>
        /// Nombre total d'utilisateurs actifs (alias de ActiveMembers)
        /// </summary>
        [JsonIgnore]
        public int TotalActiveUsers
        {
            get => ActiveMembers;
            set => ActiveMembers = value;
        }

        /// <summary>
        /// Date et heure de la dernière activité (alias de LastPostDate)
        /// </summary>
        [JsonIgnore]
        public DateTime? LastActivityAt
        {
            get => LastPostDate;
            set => LastPostDate = value;
        }

        /// <summary>
        /// Nom d'utilisateur du dernier contributeur (alias de LastActiveUsername)
        /// </summary>
        [JsonIgnore]
        public string? LastActivityByUsername
        {
            get => LastActiveUsername;
            set => LastActiveUsername = value;
        }
        
        /// <summary>
        /// Date du dernier message
        /// </summary>
        public DateTime? LastPostDate { get; set; }
        
        /// <summary>
        /// Nom d'utilisateur du dernier contributeur
        /// </summary>
        public string? LastActiveUsername { get; set; }
    }
}

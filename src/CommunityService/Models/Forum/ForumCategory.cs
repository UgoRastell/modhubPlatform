using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Forum
{
    /// <summary>
    /// Catégorie de forum
    /// </summary>
    public class ForumCategory
    {
        /// <summary>
        /// Identifiant unique de la catégorie
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom de la catégorie
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Description de la catégorie
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de l'icône de la catégorie
        /// </summary>
        public string IconUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Si la catégorie est liée à un jeu spécifique
        /// </summary>
        public string? GameId { get; set; }
        
        /// <summary>
        /// Si la catégorie est liée à un mod spécifique
        /// </summary>
        public string? ModId { get; set; }
        
        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int SortOrder { get; set; } = 0;
        
        /// <summary>
        /// ID de la catégorie parente (si sous-catégorie)
        /// </summary>
        public string? ParentCategoryId { get; set; }
        
        /// <summary>
        /// Permissions spéciales pour cette catégorie
        /// </summary>
        public ForumCategoryPermissions Permissions { get; set; } = new ForumCategoryPermissions();
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Date de dernière mise à jour
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Statistiques de la catégorie
        /// </summary>
        public ForumCategoryStats Stats { get; set; } = new ForumCategoryStats();
    }
    
    /// <summary>
    /// Permissions spéciales pour une catégorie de forum
    /// </summary>
    public class ForumCategoryPermissions
    {
        /// <summary>
        /// Rôles autorisés à voir cette catégorie
        /// </summary>
        public List<string> ViewRoles { get; set; } = new List<string>();
        
        /// <summary>
        /// Rôles autorisés à créer des sujets dans cette catégorie
        /// </summary>
        public List<string> CreateTopicRoles { get; set; } = new List<string>();
        
        /// <summary>
        /// Rôles autorisés à répondre aux sujets dans cette catégorie
        /// </summary>
        public List<string> ReplyRoles { get; set; } = new List<string>();
        
        /// <summary>
        /// Rôles autorisés à modérer cette catégorie
        /// </summary>
        public List<string> ModerateRoles { get; set; } = new List<string> { "Admin", "Moderator" };
    }
    
    /// <summary>
    /// Statistiques d'une catégorie de forum
    /// </summary>
    public class ForumCategoryStats
    {
        /// <summary>
        /// Nombre total de sujets
        /// </summary>
        public int TopicCount { get; set; } = 0;
        
        /// <summary>
        /// Nombre total de messages
        /// </summary>
        public int PostCount { get; set; } = 0;
        
        /// <summary>
        /// Date du dernier message
        /// </summary>
        public DateTime? LastPostDate { get; set; }
        
        /// <summary>
        /// ID de l'utilisateur du dernier message
        /// </summary>
        public string? LastPostUserId { get; set; }
        
        /// <summary>
        /// Nom d'utilisateur du dernier message
        /// </summary>
        public string? LastPostUsername { get; set; }
        
        /// <summary>
        /// ID du dernier sujet actif
        /// </summary>
        public string? LastTopicId { get; set; }
        
        /// <summary>
        /// Titre du dernier sujet actif
        /// </summary>
        public string? LastTopicTitle { get; set; }
    }
}

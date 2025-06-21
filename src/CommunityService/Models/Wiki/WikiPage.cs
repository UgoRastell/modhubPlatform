using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace CommunityService.Models.Wiki
{
    /// <summary>
    /// Page de wiki pour la documentation collaborative
    /// </summary>
    public class WikiPage
    {
        /// <summary>
        /// Identifiant unique de la page
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Titre de la page
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Slug URL pour accéder à la page (ex: getting-started)
        /// </summary>
        public string Slug { get; set; } = string.Empty;
        
        /// <summary>
        /// Contenu de la page
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Format du contenu (Markdown, HTML)
        /// </summary>
        public string ContentFormat { get; set; } = "Markdown";
        
        /// <summary>
        /// Catégorie de la page
        /// </summary>
        public string Category { get; set; } = string.Empty;
        
        /// <summary>
        /// Sous-catégorie de la page
        /// </summary>
        public string? Subcategory { get; set; }
        
        /// <summary>
        /// Si cette page est liée à un mod spécifique
        /// </summary>
        public string? ModId { get; set; }
        
        /// <summary>
        /// Si cette page est liée à un jeu spécifique
        /// </summary>
        public string? GameId { get; set; }
        
        /// <summary>
        /// ID du parent dans la hiérarchie (si c'est une sous-page)
        /// </summary>
        public string? ParentPageId { get; set; }
        
        /// <summary>
        /// Ordre d'affichage dans la navigation
        /// </summary>
        public int SortOrder { get; set; } = 0;
        
        /// <summary>
        /// ID de l'utilisateur qui a créé la page
        /// </summary>
        public string CreatedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur du créateur
        /// </summary>
        public string CreatedByUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// ID de l'utilisateur qui a fait la dernière modification
        /// </summary>
        public string LastModifiedByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur du dernier modificateur
        /// </summary>
        public string LastModifiedByUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Si la page nécessite un compte utilisateur pour être consultée
        /// </summary>
        public bool RequiresAuthentication { get; set; } = false;
        
        /// <summary>
        /// Si la page est publiée ou en brouillon
        /// </summary>
        public bool IsPublished { get; set; } = true;
        
        /// <summary>
        /// Si la page est officiellement validée par l'équipe
        /// </summary>
        public bool IsVerified { get; set; } = false;
        
        /// <summary>
        /// Tags pour faciliter la recherche
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();
        
        /// <summary>
        /// URL vers des ressources externes associées
        /// </summary>
        public List<WikiExternalResource>? ExternalResources { get; set; }
        
        /// <summary>
        /// Pages liées manuellement à celle-ci
        /// </summary>
        public List<WikiRelatedPage>? RelatedPages { get; set; }
        
        /// <summary>
        /// Historique des révisions
        /// </summary>
        public List<WikiRevision>? Revisions { get; set; }
        
        /// <summary>
        /// Statistiques de la page
        /// </summary>
        public WikiPageStats Stats { get; set; } = new WikiPageStats();
    }
    
    /// <summary>
    /// Révision d'une page wiki
    /// </summary>
    public class WikiRevision
    {
        /// <summary>
        /// ID unique de la révision
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Version du contenu
        /// </summary>
        public string Content { get; set; } = string.Empty;
        
        /// <summary>
        /// Résumé des modifications
        /// </summary>
        public string? ChangeDescription { get; set; }
        
        /// <summary>
        /// ID de l'utilisateur ayant fait la révision
        /// </summary>
        public string RevisionByUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nom d'utilisateur de l'auteur de la révision
        /// </summary>
        public string RevisionByUsername { get; set; } = string.Empty;
        
        /// <summary>
        /// Date de la révision
        /// </summary>
        public DateTime RevisionDate { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Ressource externe liée à une page wiki
    /// </summary>
    public class WikiExternalResource
    {
        /// <summary>
        /// Titre de la ressource
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// URL de la ressource externe
        /// </summary>
        public string Url { get; set; } = string.Empty;
        
        /// <summary>
        /// Type de ressource (vidéo, documentation, tutoriel, etc.)
        /// </summary>
        public string Type { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Page liée manuellement à une autre page wiki
    /// </summary>
    public class WikiRelatedPage
    {
        /// <summary>
        /// ID de la page liée
        /// </summary>
        public string PageId { get; set; } = string.Empty;
        
        /// <summary>
        /// Titre de la page liée (pour affichage)
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// Description de la relation
        /// </summary>
        public string? RelationDescription { get; set; }
    }
    
    /// <summary>
    /// Statistiques d'une page wiki
    /// </summary>
    public class WikiPageStats
    {
        /// <summary>
        /// Nombre de vues
        /// </summary>
        public int ViewCount { get; set; } = 0;
        
        /// <summary>
        /// Nombre de contributions différentes
        /// </summary>
        public int ContributionCount { get; set; } = 0;
        
        /// <summary>
        /// Nombre d'utilisateurs qui ont marqué cette page comme utile
        /// </summary>
        public int HelpfulVotes { get; set; } = 0;
        
        /// <summary>
        /// Nombre de commentaires
        /// </summary>
        public int CommentCount { get; set; } = 0;
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityService.Models.Wiki;

namespace CommunityService.Services.Wiki
{
    /// <summary>
    /// Interface pour le service de gestion du wiki collaboratif
    /// </summary>
    public interface IWikiService
    {
        /// <summary>
        /// Récupère toutes les pages wiki
        /// </summary>
        Task<List<WikiPage>> GetAllPagesAsync(int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère une page wiki par son ID
        /// </summary>
        Task<WikiPage> GetPageByIdAsync(string pageId);
        
        /// <summary>
        /// Récupère une page wiki par son slug d'URL
        /// </summary>
        Task<WikiPage> GetPageBySlugAsync(string slug);
        
        /// <summary>
        /// Récupère les pages wiki pour un mod spécifique
        /// </summary>
        Task<List<WikiPage>> GetPagesByModIdAsync(string modId);
        
        /// <summary>
        /// Récupère les pages wiki pour un jeu spécifique
        /// </summary>
        Task<List<WikiPage>> GetPagesByGameIdAsync(string gameId);
        
        /// <summary>
        /// Récupère les pages wiki par catégorie
        /// </summary>
        Task<List<WikiPage>> GetPagesByCategoryAsync(string category, string subcategory = null);
        
        /// <summary>
        /// Recherche des pages wiki par mot-clé
        /// </summary>
        Task<List<WikiPage>> SearchPagesAsync(string query);
        
        /// <summary>
        /// Crée une nouvelle page wiki
        /// </summary>
        Task<WikiPage> CreatePageAsync(WikiPage page);
        
        /// <summary>
        /// Met à jour une page wiki existante
        /// </summary>
        Task<bool> UpdatePageAsync(string pageId, WikiPage page, string changeDescription);
        
        /// <summary>
        /// Supprime une page wiki
        /// </summary>
        Task<bool> DeletePageAsync(string pageId);
        
        /// <summary>
        /// Récupère toutes les révisions d'une page wiki
        /// </summary>
        Task<List<WikiRevision>> GetPageRevisionsAsync(string pageId);
        
        /// <summary>
        /// Récupère une révision spécifique d'une page wiki
        /// </summary>
        Task<WikiRevision> GetRevisionByIdAsync(string pageId, string revisionId);
        
        /// <summary>
        /// Restaure une page à une révision précédente
        /// </summary>
        Task<bool> RestorePageToRevisionAsync(string pageId, string revisionId, string changeDescription);
        
        /// <summary>
        /// Ajoute une ressource externe à une page wiki
        /// </summary>
        Task<bool> AddExternalResourceAsync(string pageId, WikiExternalResource resource);
        
        /// <summary>
        /// Supprime une ressource externe d'une page wiki
        /// </summary>
        Task<bool> RemoveExternalResourceAsync(string pageId, string resourceTitle);
        
        /// <summary>
        /// Ajoute une page liée à une page wiki
        /// </summary>
        Task<bool> AddRelatedPageAsync(string pageId, WikiRelatedPage relatedPage);
        
        /// <summary>
        /// Supprime une page liée d'une page wiki
        /// </summary>
        Task<bool> RemoveRelatedPageAsync(string pageId, string relatedPageId);
        
        /// <summary>
        /// Marque une page comme vérifiée/officielle
        /// </summary>
        Task<bool> VerifyPageAsync(string pageId, bool isVerified);
        
        /// <summary>
        /// Marque une page comme utile (vote)
        /// </summary>
        Task<bool> VoteHelpfulAsync(string pageId, string userId);
        
        /// <summary>
        /// Récupère les pages wiki les plus consultées
        /// </summary>
        Task<List<WikiPage>> GetMostViewedPagesAsync(int count = 5);
        
        /// <summary>
        /// Récupère les pages wiki récemment mises à jour
        /// </summary>
        Task<List<WikiPage>> GetRecentlyUpdatedPagesAsync(int count = 5);
        
        /// <summary>
        /// Récupère les statistiques générales du wiki
        /// </summary>
        Task<WikiStatistics> GetWikiStatisticsAsync();
    }
    
    /// <summary>
    /// Statistiques générales du wiki
    /// </summary>
    public class WikiStatistics
    {
        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages { get; set; }
        
        /// <summary>
        /// Nombre total de révisions
        /// </summary>
        public int TotalRevisions { get; set; }
        
        /// <summary>
        /// Nombre de contributeurs uniques
        /// </summary>
        public int UniqueContributors { get; set; }
        
        /// <summary>
        /// Nombre de pages vérifiées/officielles
        /// </summary>
        public int VerifiedPages { get; set; }
        
        /// <summary>
        /// Date de dernière mise à jour du wiki
        /// </summary>
        public DateTime? LastUpdateDate { get; set; }
        
        /// <summary>
        /// Nom du dernier contributeur
        /// </summary>
        public string? LastContributorName { get; set; }
    }
}

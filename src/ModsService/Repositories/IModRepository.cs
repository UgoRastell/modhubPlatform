using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModsService.Models;

namespace ModsService.Repositories
{
    /// <summary>
    /// Interface pour le repository des mods
    /// </summary>
    public interface IModRepository
    {
        /// <summary>
        /// Récupère tous les mods
        /// </summary>
        Task<IEnumerable<Mod>> GetAllModsAsync();
        
        /// <summary>
        /// Récupère un mod par son ID
        /// </summary>
        Task<Mod> GetModByIdAsync(string id);
        
        /// <summary>
        /// Récupère les mods par ID de jeu
        /// </summary>
        Task<IEnumerable<Mod>> GetModsByGameIdAsync(string gameId);
        
        /// <summary>
        /// Récupère les mods par ID de créateur
        /// </summary>
        Task<IEnumerable<Mod>> GetModsByCreatorIdAsync(string creatorId);
        
        /// <summary>
        /// Crée un nouveau mod
        /// </summary>
        Task<Mod> CreateModAsync(Mod mod);
        
        /// <summary>
        /// Met à jour un mod existant
        /// </summary>
        Task<bool> UpdateModAsync(Mod mod);
        
        /// <summary>
        /// Supprime un mod par son ID
        /// </summary>
        Task<bool> DeleteModAsync(string id);
        
        /// <summary>
        /// Recherche des mods avec filtres et pagination
        /// </summary>
        Task<(IEnumerable<Mod> Mods, int TotalCount)> SearchModsAsync(
            string gameId = null,
            string categoryId = null,
            string searchTerm = null,
            string sortBy = null,
            int pageNumber = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Ajoute une note à un mod
        /// </summary>
        Task<bool> AddRatingAsync(string modId, int rating, string userId);
        
        /// <summary>
        /// Recherche avancée de mods avec filtres multiples
        /// </summary>
        Task<(IEnumerable<Mod> Mods, int TotalCount)> SearchModsAdvancedAsync(
            ModSearchFilter filter, 
            ModSortOptions sortOptions,
            int pageNumber = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Recherche de mods par tags
        /// </summary>
        Task<(IEnumerable<Mod> Mods, int TotalCount)> FindModsByTagsAsync(
            IEnumerable<string> tags,
            string matchType = "any",
            ModSortOptions sortOptions = null,
            int pageNumber = 1,
            int pageSize = 20);
        
        /// <summary>
        /// Récupère les mods les plus populaires
        /// </summary>
        Task<IEnumerable<Mod>> GetPopularModsAsync(
            string gameId = null,
            string categoryId = null,
            DateTime? since = null,
            int limit = 10);
        
        /// <summary>
        /// Récupère les mods récemment mis à jour
        /// </summary>
        Task<IEnumerable<Mod>> GetRecentlyUpdatedModsAsync(
            string gameId = null, 
            int limit = 10);
        
        /// <summary>
        /// Recherche de mods compatibles avec une version spécifique d'un jeu
        /// </summary>
        Task<(IEnumerable<Mod> Mods, int TotalCount)> FindModsByGameVersionCompatibilityAsync(
            string gameId,
            string gameVersion,
            ModSortOptions sortOptions = null,
            int pageNumber = 1,
            int pageSize = 20);
    }
}

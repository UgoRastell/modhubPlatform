using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ModsService.Models;
using ModsService.Repositories;

namespace ModsService.Services
{
    /// <summary>
    /// Interface pour le service de gestion des mods
    /// </summary>
    public interface IModService
    {
        /// <summary>
        /// Récupère tous les mods avec pagination et filtrage optionnel
        /// </summary>
        Task<PaginatedList<Mod>> GetModsAsync(ModSearchFilter filter = null, ModSortOptions sortOptions = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère un mod par son identifiant
        /// </summary>
        Task<Mod> GetModByIdAsync(string id);
        
        /// <summary>
        /// Récupère les mods d'un créateur
        /// </summary>
        Task<IEnumerable<Mod>> GetModsByCreatorAsync(string creatorId);
        
        /// <summary>
        /// Crée un nouveau mod
        /// </summary>
        Task<Mod> CreateModAsync(Mod mod);
        
        /// <summary>
        /// Met à jour un mod existant
        /// </summary>
        Task<Mod> UpdateModAsync(string id, Mod mod);
        
        /// <summary>
        /// Supprime un mod
        /// </summary>
        Task<bool> DeleteModAsync(string id);
        
        /// <summary>
        /// Ajoute une nouvelle version à un mod existant
        /// </summary>
        Task<ModVersion> AddModVersionAsync(string modId, ModVersion version);
        
        /// <summary>
        /// Met à jour une version existante d'un mod
        /// </summary>
        Task<ModVersion> UpdateModVersionAsync(string modId, string versionId, ModVersion version);
        
        /// <summary>
        /// Supprime une version d'un mod
        /// </summary>
        Task<bool> DeleteModVersionAsync(string modId, string versionId);
        
        /// <summary>
        /// Télécharge un fichier mod et l'associe à une version
        /// </summary>
        Task<ModFile> UploadModFileAsync(string modId, string versionId, IFormFile file);
        
        /// <summary>
        /// Récupère un fichier mod
        /// </summary>
        Task<ModFile> GetModFileAsync(string modId, string versionId);
        
        /// <summary>
        /// Met à jour le statut d'un mod (brouillon, publié, archivé, etc.)
        /// </summary>
        Task<Mod> UpdateModStatusAsync(string modId, ModStatus status);
        
        /// <summary>
        /// Recherche des mods par mot-clé, catégorie, tag, etc.
        /// </summary>
        Task<PaginatedList<Mod>> SearchModsAsync(string query, ModSearchFilter filter = null, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Génère le changelog entre deux versions d'un mod
        /// </summary>
        Task<string> GenerateChangelogAsync(string modId, string fromVersion, string toVersion);
    }
    
    /// <summary>
    /// Classe pour la liste paginée
    /// </summary>
    public class PaginatedList<T>
    {
        /// <summary>
        /// Les items de la page courante
        /// </summary>
        public IEnumerable<T> Items { get; set; }
        
        /// <summary>
        /// Le nombre total d'items
        /// </summary>
        public int TotalCount { get; set; }
        
        /// <summary>
        /// Le numéro de la page courante
        /// </summary>
        public int CurrentPage { get; set; }
        
        /// <summary>
        /// Le nombre d'items par page
        /// </summary>
        public int PageSize { get; set; }
        
        /// <summary>
        /// Le nombre total de pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        
        /// <summary>
        /// Indique s'il y a une page précédente
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;
        
        /// <summary>
        /// Indique s'il y a une page suivante
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}

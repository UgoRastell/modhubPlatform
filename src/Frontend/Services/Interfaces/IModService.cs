using Frontend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Interfaces
{
    /// <summary>
    /// Interface définissant les services liés aux mods
    /// </summary>
    public interface IModService
    {
        /// <summary>
        /// Récupère tous les mods disponibles dans la marketplace
        /// </summary>
        Task<List<Mod>> GetAllModsAsync();
        
        /// <summary>
        /// Récupère les mods favoris de l'utilisateur
        /// </summary>
        Task<List<Mod>> GetFavoriteModsAsync(string userId);
        
        /// <summary>
        /// Récupère les mods de l'utilisateur (bibliothèque)
        /// </summary>
        Task<List<ModItem>> GetUserModsAsync(string userId, int page = 1, int pageSize = 20);
        
        /// <summary>
        /// Récupère un mod par son identifiant
        /// </summary>
        Task<Mod> GetModByIdAsync(string modId);
        
        /// <summary>
        /// Récupère les mods créés par un utilisateur
        /// </summary>
        Task<List<Models.ModManagement.ModInfo>> GetCreatorModsAsync(string creatorId, string status = null);
        
        /// <summary>
        /// Ajoute un mod aux favoris de l'utilisateur
        /// </summary>
        Task<bool> AddToFavoritesAsync(string userId, string modId);
        
        /// <summary>
        /// Retire un mod des favoris de l'utilisateur
        /// </summary>
        Task<bool> RemoveFromFavoritesAsync(string userId, string modId);
        
        /// <summary>
        /// Télécharge un mod
        /// </summary>
        Task<string> DownloadModAsync(string modId);
    }
}

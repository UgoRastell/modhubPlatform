using ModsService.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModsService.Repositories
{
    public interface IModRepository
    {
        Task<List<Mod>> GetAllAsync(int page = 1, int pageSize = 50, string sortBy = "recent");
        Task<Mod> GetByIdAsync(string id);
        Task<List<Mod>> GetByCreatorIdAsync(string creatorId);
        Task<int> GetTotalCountAsync();
        Task CreateAsync(Mod mod);
        
        /// <summary>
        /// Met à jour la note moyenne et le nombre d'avis pour un mod.
        /// </summary>
        Task UpdateRatingAsync(string id, double averageRating, int reviewCount);
        
        /// <summary>
        /// Incrémente le compteur de téléchargements d'un mod de façon atomique et persistante.
        /// </summary>
        Task IncrementDownloadCountAsync(string id);
        
        /// <summary>
        /// Supprime de manière permanente un mod et retourne vrai si la suppression a eu lieu.
        /// </summary>
        Task<bool> DeleteAsync(string id);
    }
}

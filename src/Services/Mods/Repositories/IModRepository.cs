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
    }
}

using Frontend.Models.Common;
using Frontend.Models.Forum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Forum
{
    public interface IForumService
    {
        Task<List<ForumCategoryViewModel>> GetCategoriesAsync();
        Task<ForumCategoryViewModel> GetCategoryByIdAsync(string id);
        Task<List<ForumTopicViewModel>> GetTopicsByCategoryIdAsync(string categoryId);
        Task<ForumStatistics> GetForumStatisticsAsync();
        
        // Méthodes manquantes détectées dans les pages Forum
        Task<List<ForumCategoryViewModel>> GetAllCategoriesAsync();
        Task<List<string>> GetPopularTagsAsync();
        Task<PagedResult<ForumTopicViewModel>> GetTopicsByCategoryAsync(string categoryId, int page = 1, int pageSize = 10, string sortBy = "recent", string filterType = "");
    }
}

using Frontend.Models.Wiki;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Wiki
{
    public interface IWikiService
    {
        Task<List<WikiCategoryViewModel>> GetCategoriesAsync();
        Task<WikiPageViewModel> GetPageByIdAsync(string id);
        Task<List<WikiPageViewModel>> GetPagesByCategoryIdAsync(string categoryId);
        Task<WikiStatistics> GetWikiStatisticsAsync();
        
        // Méthodes manquantes détectées dans WikiIndex.razor
        Task<List<WikiPageViewModel>> GetFeaturedPagesAsync();
        Task<List<WikiPageViewModel>> GetRecentPagesAsync(int count);
        Task<List<WikiCategoryViewModel>> GetAllCategoriesAsync();
        Task<List<string>> GetPopularTagsAsync();
    }
}

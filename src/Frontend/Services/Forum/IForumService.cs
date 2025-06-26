using Frontend.Models.Common;
using Frontend.Models.Forum;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frontend.Services.Forum
{
    public interface IForumService
    {
        // Catégories - CRUD complet
        Task<List<ForumCategoryViewModel>> GetCategoriesAsync();
        Task<List<ForumCategoryViewModel>> GetAllCategoriesAsync();
        Task<ForumCategoryViewModel> GetCategoryByIdAsync(string id);
        Task<ForumCategoryViewModel> CreateCategoryAsync(CreateForumCategoryDto categoryDto);
        Task<ForumCategoryViewModel> UpdateCategoryAsync(UpdateForumCategoryDto categoryDto);
        Task<bool> DeleteCategoryAsync(string id);
        
        // Topics - CRUD complet
        Task<List<ForumTopicViewModel>> GetTopicsByCategoryIdAsync(string categoryId);
        Task<PagedResult<ForumTopicViewModel>> GetTopicsByCategoryAsync(string categoryId, int page = 1, int pageSize = 10, string sortBy = "recent", string filterType = "");
        Task<ForumTopicViewModel> GetTopicByIdAsync(string topicId);
        Task<ForumTopicViewModel> CreateTopicAsync(CreateForumTopicDto topicDto);
        Task<ForumTopicViewModel> UpdateTopicAsync(UpdateForumTopicDto topicDto);
        Task<bool> DeleteTopicAsync(string id);
        Task<bool> PinTopicAsync(string id, bool isPinned);
        Task<bool> LockTopicAsync(string id, bool isLocked);
        
        // Posts - CRUD complet
        Task<List<ForumPostViewModel>> GetPostsByTopicIdAsync(string topicId, int page = 1, int pageSize = 20);
        Task<ForumPostViewModel> GetPostByIdAsync(string postId);
        Task<ForumPostViewModel> CreatePostAsync(CreateForumPostDto postDto);
        Task<ForumPostViewModel> UpdatePostAsync(UpdateForumPostDto postDto);
        Task<bool> DeletePostAsync(string id);
        Task<bool> LikePostAsync(string postId, bool isLiked);
        
        // Recherche et statistiques
        Task<PagedResult<ForumTopicViewModel>> SearchTopicsAsync(string query, string? categoryId = null, string? tag = null, int page = 1, int pageSize = 10);
        Task<List<string>> GetPopularTagsAsync();
        Task<ForumStatistics> GetForumStatisticsAsync();
        
        // Utilitaires
        Task<bool> CanUserEditPostAsync(string postId, string userId);
        Task<bool> CanUserDeletePostAsync(string postId, string userId);
        Task<bool> CanUserManageCategoryAsync(string userId);
    }
}

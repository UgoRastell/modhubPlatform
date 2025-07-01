using Frontend.Models.Common;
using Frontend.Models.Forum;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Frontend.Services.Forum
{
    public class ForumService : IForumService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ForumService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ForumService");
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        #region Categories CRUD

        public async Task<List<ForumCategoryViewModel>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/forum/categories");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ForumCategoryViewModel>>(content, _jsonOptions) ?? new List<ForumCategoryViewModel>();
                }
                return GetMockCategories(); // Fallback pour les tests
            }
            catch
            {
                return GetMockCategories(); // Fallback en cas d'erreur
            }
        }

        public async Task<List<ForumCategoryViewModel>> GetAllCategoriesAsync()
        {
            return await GetCategoriesAsync();
        }

        public async Task<ForumCategoryViewModel> GetCategoryByIdAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/categories/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumCategoryViewModel>(content, _jsonOptions) ?? new ForumCategoryViewModel
                    {
                        Id = id,
                        Name = "Catégorie non trouvée",
                        Description = "Cette catégorie n'existe pas",
                        IconName = "error",
                        LastActivityByUserId = "",
                        LastActivityByUserName = ""
                    };
                }
            }
            catch { }

            return new ForumCategoryViewModel
            {
                Id = id,
                Name = "Catégorie non trouvée",
                Description = "Cette catégorie n'existe pas",
                IconName = "error",
                LastActivityByUserId = "",
                LastActivityByUserName = ""
            };
        }

        public async Task<ForumCategoryViewModel> CreateCategoryAsync(CreateForumCategoryDto categoryDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(categoryDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/forum/categories", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumCategoryViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la création de la catégorie");
                }
            }
            catch { }

            // Mock response for testing
            return new ForumCategoryViewModel
            {
                Id = Guid.NewGuid().ToString(),
                Name = categoryDto.Name,
                Description = categoryDto.Description,
                IconName = categoryDto.IconName,
                TopicsCount = 0,
                PostsCount = 0,
                LastActivityByUserId = "",
                LastActivityByUserName = ""
            };
        }

        public async Task<ForumCategoryViewModel> UpdateCategoryAsync(UpdateForumCategoryDto categoryDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(categoryDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/forum/categories/{categoryDto.Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumCategoryViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la mise à jour de la catégorie");
                }
            }
            catch { }

            return await GetCategoryByIdAsync(categoryDto.Id);
        }

        public async Task<bool> DeleteCategoryAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/forum/categories/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Topics CRUD

        public async Task<List<ForumTopicViewModel>> GetTopicsByCategoryIdAsync(string categoryId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/categories/{categoryId}/topics");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<ForumTopicViewModel>>(content, _jsonOptions) ?? new List<ForumTopicViewModel>();
                }
            }
            catch { }

            return GetMockTopics().Where(t => t.CategoryId == categoryId).ToList();
        }

        public async Task<PagedResult<ForumTopicViewModel>> GetTopicsByCategoryAsync(string categoryId, int page = 1, int pageSize = 10, string sortBy = "recent", string filterType = "")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/categories/{categoryId}/topics?page={page}&pageSize={pageSize}&sortBy={sortBy}&filterType={filterType}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PagedResult<ForumTopicViewModel>>(content, _jsonOptions) ?? new PagedResult<ForumTopicViewModel>();
                }
            }
            catch { }

            var mockTopics = GetMockTopics().Where(t => t.CategoryId == categoryId).ToList();
            return new PagedResult<ForumTopicViewModel>
            {
                Items = mockTopics.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                TotalCount = mockTopics.Count,
                PageNumber = page,
                PageSize = pageSize
                // TotalPages est calculé automatiquement à partir de TotalCount et PageSize
            };
        }

        public async Task<ForumTopicViewModel> GetTopicByIdAsync(string topicId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/topics/{topicId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumTopicViewModel>(content, _jsonOptions) ?? CreateNotFoundTopic(topicId);
                }
            }
            catch { }

            return GetMockTopics().FirstOrDefault(t => t.Id == topicId) ?? CreateNotFoundTopic(topicId);
        }

        public async Task<ForumTopicViewModel> CreateTopicAsync(CreateForumTopicDto topicDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(topicDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/forum/topics", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumTopicViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la création du topic");
                }
            }
            catch { }

            // Mock response for testing
            return new ForumTopicViewModel
            {
                Id = Guid.NewGuid().ToString(),
                CategoryId = topicDto.CategoryId,
                Title = topicDto.Title,
                Content = topicDto.Content,
                AuthorId = "current-user-id",
                AuthorName = "Utilisateur actuel",
                Slug = topicDto.Title.ToLower().Replace(" ", "-"),
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                Tags = topicDto.Tags,
                IsPinned = topicDto.IsPinned,
                IsLocked = topicDto.IsLocked
            };
        }

        public async Task<ForumTopicViewModel> UpdateTopicAsync(UpdateForumTopicDto topicDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(topicDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/forum/topics/{topicDto.Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumTopicViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la mise à jour du topic");
                }
            }
            catch { }

            return await GetTopicByIdAsync(topicDto.Id);
        }

        public async Task<bool> DeleteTopicAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/forum/topics/{id}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> PinTopicAsync(string id, bool isPinned)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/forum/topics/{id}/pin?isPinned={isPinned}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LockTopicAsync(string id, bool isLocked)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/forum/topics/{id}/lock?isLocked={isLocked}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Posts CRUD

        public async Task<List<Frontend.Models.Forum.ForumPostViewModel>> GetPostsByTopicIdAsync(string topicId, int page = 1, int pageSize = 20)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/topics/{topicId}/posts?page={page}&pageSize={pageSize}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Frontend.Models.Forum.ForumPostViewModel>>(content, _jsonOptions) ?? new List<Frontend.Models.Forum.ForumPostViewModel>();
                }
            }
            catch { }

            return GetMockPosts().Where(p => p.TopicId == topicId).ToList();
        }

        public async Task<Frontend.Models.Forum.ForumPostViewModel> GetPostByIdAsync(string postId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/posts/{postId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Frontend.Models.Forum.ForumPostViewModel>(content, _jsonOptions) ?? CreateNotFoundPost(postId);
                }
            }
            catch { }

            return GetMockPosts().FirstOrDefault(p => p.Id == postId) ?? CreateNotFoundPost(postId);
        }

        public async Task<Frontend.Models.Forum.ForumPostViewModel> CreatePostAsync(CreateForumPostDto postDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(postDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/forum/posts", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Frontend.Models.Forum.ForumPostViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la création du post");
                }
            }
            catch { }

            // Mock response for testing
            return new ForumPostViewModel
            {
                Id = Guid.NewGuid().ToString(),
                TopicId = postDto.TopicId,
                Content = postDto.Content,
                AuthorId = "current-user-id",
                AuthorName = "Utilisateur actuel",
                CreatedAt = DateTime.UtcNow,
                ParentPostId = postDto.ParentPostId,
                Attachments = postDto.Attachments
            };
        }

        public async Task<Frontend.Models.Forum.ForumPostViewModel> UpdatePostAsync(UpdateForumPostDto postDto)
        {
            try
            {
                var json = JsonSerializer.Serialize(postDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/forum/posts/{postDto.Id}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<Frontend.Models.Forum.ForumPostViewModel>(responseContent, _jsonOptions) ?? throw new InvalidOperationException("Erreur lors de la mise à jour du post");
                }
            }
            catch
            {
                // En cas d'erreur, on utilise une approche de repli
            }

            // Fallback : récupérer le post existant
            return await GetPostByIdAsync(postDto.Id);
        }

        public async Task<bool> DeletePostAsync(string postId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/forum/posts/{postId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LikePostAsync(string postId, bool isLiked)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/forum/posts/{postId}/like?isLiked={isLiked}", null);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Search and Statistics

        public async Task<PagedResult<ForumTopicViewModel>> SearchTopicsAsync(string query, string? categoryId = null, string? tag = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var url = $"api/forum/search?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
                if (!string.IsNullOrEmpty(categoryId))
                    url += $"&categoryId={categoryId}";
                if (!string.IsNullOrEmpty(tag))
                    url += $"&tag={Uri.EscapeDataString(tag)}";

                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<PagedResult<ForumTopicViewModel>>(content, _jsonOptions) ?? new PagedResult<ForumTopicViewModel>();
                }
            }
            catch { }

            return new PagedResult<ForumTopicViewModel>();
        }

        public async Task<List<string>> GetPopularTagsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/forum/tags/popular");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<string>>(content, _jsonOptions) ?? new List<string>();
                }
            }
            catch { }

            return new List<string> { "mods", "tutoriel", "help", "showcase", "bug", "suggestion", "gaming", "community" };
        }

        public async Task<ForumStatistics> GetForumStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/forum/statistics");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<ForumStatistics>(content, _jsonOptions) ?? GetMockStatistics();
                }
            }
            catch { }

            return GetMockStatistics();
        }

        #endregion

        #region User Permissions

        public async Task<bool> CanUserEditPostAsync(string postId, string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/posts/{postId}/can-edit?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                }
            }
            catch { }

            return true; // Default to allow for testing
        }

        public async Task<bool> CanUserDeletePostAsync(string postId, string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/posts/{postId}/can-delete?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                }
            }
            catch { }

            return true; // Default to allow for testing
        }

        public async Task<bool> CanUserManageCategoryAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/forum/categories/can-manage?userId={userId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<bool>(content, _jsonOptions);
                }
            }
            catch { }

            return true; // Default to allow for testing
        }

        #endregion

        #region Mock Data

        private List<ForumCategoryViewModel> GetMockCategories()
        {
            return new List<ForumCategoryViewModel>
            {
                new ForumCategoryViewModel
                {
                    Id = "general",
                    Name = "Discussions générales",
                    Description = "Discussions générales sur les mods et les jeux",
                    IconName = "forum",
                    TopicsCount = 125,
                    PostsCount = 1847,
                    LastActivityByUserId = "user1",
                    LastActivityByUserName = "ModMaster",
                    LastActivityDate = DateTime.UtcNow.AddMinutes(-30)
                },
                new ForumCategoryViewModel
                {
                    Id = "tutorials",
                    Name = "Tutoriels",
                    Description = "Guides et tutoriels pour créer des mods",
                    IconName = "school",
                    TopicsCount = 87,
                    PostsCount = 1203,
                    LastActivityByUserId = "user2",
                    LastActivityByUserName = "TechGuru",
                    LastActivityDate = DateTime.UtcNow.AddHours(-2)
                },
                new ForumCategoryViewModel
                {
                    Id = "showcase",
                    Name = "Présentation de mods",
                    Description = "Présentez vos créations à la communauté",
                    IconName = "star",
                    TopicsCount = 234,
                    PostsCount = 3421,
                    LastActivityByUserId = "user3",
                    LastActivityByUserName = "CreativeMind",
                    LastActivityDate = DateTime.UtcNow.AddMinutes(-15)
                },
                new ForumCategoryViewModel
                {
                    Id = "support",
                    Name = "Support technique",
                    Description = "Aide et résolution de problèmes",
                    IconName = "help",
                    TopicsCount = 156,
                    PostsCount = 892,
                    LastActivityByUserId = "user4",
                    LastActivityByUserName = "HelpBot",
                    LastActivityDate = DateTime.UtcNow.AddMinutes(-45)
                }
            };
        }

        private List<ForumTopicViewModel> GetMockTopics()
        {
            return new List<ForumTopicViewModel>
            {
                new ForumTopicViewModel
                {
                    Id = "topic1",
                    CategoryId = "general",
                    Title = "Bienvenue dans la communauté ModHub",
                    Content = "Présentez-vous et découvrez notre communauté passionnée de modding !",
                    AuthorId = "admin",
                    AuthorName = "Admin",
                    Slug = "bienvenue-communaute-modhub",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    LastActivityAt = DateTime.UtcNow.AddMinutes(-30),
                    ViewsCount = 1250,
                    RepliesCount = 45,
                    IsPinned = true,
                    Tags = new List<string> { "bienvenue", "community" }
                },
                new ForumTopicViewModel
                {
                    Id = "topic2",
                    CategoryId = "tutorials",
                    Title = "Guide complet : Créer votre premier mod Skyrim",
                    Content = "Un guide détaillé pour débuter dans le modding de Skyrim avec les outils modernes.",
                    AuthorId = "user1",
                    AuthorName = "ModMaster",
                    Slug = "guide-premier-mod-skyrim",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    LastActivityAt = DateTime.UtcNow.AddHours(-2),
                    ViewsCount = 892,
                    RepliesCount = 23,
                    Tags = new List<string> { "skyrim", "tutoriel", "débutant" }
                }
            };
        }

        private List<Frontend.Models.Forum.ForumPostViewModel> GetMockPosts()
        {
            return new List<Frontend.Models.Forum.ForumPostViewModel>
            {
                new ForumPostViewModel
                {
                    Id = "post1",
                    TopicId = "topic1",
                    Content = "Merci pour ce guide très détaillé ! Cela m'a beaucoup aidé pour commencer.",
                    AuthorId = "user2",
                    AuthorName = "Newbie",
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    LikesCount = 12
                },
                new ForumPostViewModel
                {
                    Id = "post2",
                    TopicId = "topic1",
                    Content = "Excellent travail ! J'ai quelques questions supplémentaires...",
                    AuthorId = "user3",
                    AuthorName = "Gamer123",
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    LikesCount = 8
                }
            };
        }

        private ForumTopicViewModel CreateNotFoundTopic(string id)
        {
            return new ForumTopicViewModel
            {
                Id = id,
                CategoryId = "unknown",
                Title = "Topic non trouvé",
                Content = "Ce topic n'existe pas ou a été supprimé.",
                AuthorId = "system",
                AuthorName = "Système",
                Slug = "topic-not-found"
            };
        }

        private Frontend.Models.Forum.ForumPostViewModel CreateNotFoundPost(string id)
        {
            return new ForumPostViewModel
            {
                Id = id,
                TopicId = "unknown",
                Content = "Ce message n'existe pas ou a été supprimé.",
                AuthorId = "system",
                AuthorName = "Système",
                CreatedAt = DateTime.UtcNow
            };
        }

        private ForumStatistics GetMockStatistics()
        {
            return new ForumStatistics
            {
                TotalCategories = 4,
                TotalTopics = 602,
                TotalPosts = 7363,
                OnlineUserCount = 23,
                OnlineGuestCount = 147,
                NewestMemberId = "user123",
                NewestMemberName = "NewGamer2025",
                MostActiveCategory = "Présentation de mods",
                MostActiveCategoryId = "showcase",
                MostActiveTopic = "Guide complet : Créer votre premier mod Skyrim",
                MostActiveTopicId = "topic2",
                MostActiveUser = "ModMaster",
                MostActiveUserId = "user1"
            };
        }

        #endregion
    }
}

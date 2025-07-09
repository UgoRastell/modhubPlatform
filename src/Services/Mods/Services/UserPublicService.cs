using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModsService.Models.DTOs;

namespace ModsService.Services
{
    /// <summary>
    /// Implémentation de <see cref="IUserPublicService"/> utilisant un HttpClient nommé pour communiquer
    /// avec le UsersService via l'API Gateway. Les profils sont mis en cache en mémoire pour réduire la latence
    /// et la charge réseau.
    /// </summary>
    public class UserPublicService : IUserPublicService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<UserPublicService> _logger;

        private const string ClientName = "UsersService";
        private const string CachePrefix = "PublicUser_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        public UserPublicService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<UserPublicService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PublicUserDto?> GetPublicUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var cacheKey = CachePrefix + userId;
            if (_cache.TryGetValue(cacheKey, out PublicUserDto cached))
            {
                return cached;
            }

            try
            {
                var client = _httpClientFactory.CreateClient(ClientName);
                var response = await client.GetAsync($"api/users/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Échec de récupération du profil public pour l'utilisateur {UserId}: {StatusCode}", userId, response.StatusCode);
                    return null;
                }

                var dto = await response.Content.ReadFromJsonAsync<PublicUserDto>();
                if (dto != null)
                {
                    _cache.Set(cacheKey, dto, CacheDuration);
                }
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil public utilisateur {UserId}", userId);
                return null;
            }
        }
    }
}

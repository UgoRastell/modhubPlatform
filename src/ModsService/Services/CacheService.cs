using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace ModsService.Services
{
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null) where T : class;
        Task RemoveAsync(string key);
    }

    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            var cachedValue = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(cachedValue))
            {
                return null;
            }
            
            return JsonConvert.DeserializeObject<T>(cachedValue);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpiration = null) where T : class
        {
            var options = new DistributedCacheEntryOptions();
            
            if (absoluteExpiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
            }
            else
            {
                // Par défaut, on garde en cache pendant 15 minutes
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }
            
            var serializedValue = JsonConvert.SerializeObject(value);
            await _cache.SetStringAsync(key, serializedValue, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }
    }
}

using FloodRescue.Services.Interface.Cache;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Cache
{
    public class CacheService : ICacheService
    {
        //đây là 1 dạng cache để hồi cấu hình lưu thẳng vào redis thay vì ram
        private readonly IDistributedCache _cache;
        //đây là 1 instance kết nối trực tiếp với redis để thao tác nâng cao hơn với redis mà cache không làm được
        private readonly IConnectionMultiplexer _redis;
        //thời gian hết hạn mặc định của cache sau khoảng thời gian này cache sẽ tự động bị xóa
        private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);
        private readonly ILogger<CacheService> _logger;
        
        public CacheService(IDistributedCache cache, IConnectionMultiplexer redis, ILogger<CacheService> logger)
        {
            _cache = cache;
            _redis = redis;
            _logger = logger;
        }

        public async Task<bool> ExistAsync(string key)
        {
            string? jsonData = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(jsonData);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            string? cachedData = await _cache.GetStringAsync(key);

            if (string.IsNullOrEmpty(cachedData))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedData);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemovePatternAsync(string pattern)
        {
            IServer server = _redis.GetServer(_redis.GetEndPoints().First());
            IEnumerable<RedisKey> keys = server.Keys(pattern: pattern);

            int count = 0;
            foreach (RedisKey key in keys)
            {
                await _cache.RemoveAsync(key!);
                count++;
            }
            _logger.LogInformation("[CacheService - Redis] Removed {Count} keys matching pattern '{Pattern}'", count, pattern);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration
            };

            string jsonData = JsonSerializer.Serialize(value);

            await _cache.SetStringAsync(key, jsonData, options);
        }

    }
}

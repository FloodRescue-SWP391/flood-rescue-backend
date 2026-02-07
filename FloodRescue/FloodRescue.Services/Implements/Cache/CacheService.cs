using FloodRescue.Services.Interface.Cache;
using Microsoft.Extensions.Caching.Distributed;
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
        
        public CacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
        }

        /// <summary>
        /// Kiểm tra xem coi cái dòng cache đó có tồn tại hay không thông qua key
        /// </summary>
        /// <param name="key">Truyền vào key cần tìm</param>
        /// <returns></returns>
        public async Task<bool> ExistAsync(string key)
        {
            string? jsonData = await _cache.GetStringAsync(key);
            return !string.IsNullOrEmpty(jsonData);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            //Lấy chuỗi json thô từ redis;
            string? cachedData = await _cache.GetStringAsync(key);

            //nếu cache không lấy được thì trả về default của kiểu dữ liệu
            if (string.IsNullOrEmpty(cachedData))
            {
                // linh hoạt trả về default tương ứng với từng kiểu dữ liệu
                return default;
            }

            //map sang object c# 
            return JsonSerializer.Deserialize<T>(cachedData);

        }

        /// <summary>
        /// Hàm remove cache dữ liệu theo key
        /// </summary>
        /// <param name="key">Key của dòng dữ liệu đó</param>
        /// <returns></returns>
        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task RemovePatternAsync(string pattern)
        {
            //truy cập đến server của redis - tạo 1 instance ứng với con server redis
            IServer server = _redis.GetServer(_redis.GetEndPoints().First());

            //server lấy ra 1 list key theo pattern truyền vào
            IEnumerable<RedisKey> keys = server.Keys(pattern: pattern);

            //lấy ra từng key và xóa
            foreach (RedisKey key in keys)
            {
                await _cache.RemoveAsync(key!);
            }
        }

        /// <summary>
        /// Hàm để đưa dữ liệu vào cache chuyển tất cả mọi loại object sang json dạng string để có thể SetStringAsync vào redis 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Key duy nhất cho mỗi dòng dữ liệu được đưa vào redis lưu trữ</param>
        /// <param name="value">Value lưu trữ</param>
        /// <param name="expiration">Hết hạn lưu trữ của dòng dữ liệu đó</param>
        /// <returns></returns>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
            {
                //nếu developer truyền vào expiration thì ok giữ lại, không thì mặc định là _defaultExpiration 
                AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration

            };

            //map object c# sang json để lưu vào redis
            string jsonData = JsonSerializer.Serialize(value);

            await _cache.SetStringAsync(key, jsonData, options);
        }
    }
}

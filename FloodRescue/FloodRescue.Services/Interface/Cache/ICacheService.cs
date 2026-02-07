using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Cache
{
    public interface ICacheService
    {
        /// <summary>
        /// lấy data từ cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T?> GetAsync<T>(string key);

        /// <summary>
        /// Lưu data vào cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">Cache Key</param>
        /// <param name="value">Data cần cache</param>
        /// <param name="expiration">Thời gian hết hạn (mặc định set làm 5 phút)</param>
        /// <returns></returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        /// <summary>
        /// xóa cache theo ke
        /// </summary>
        /// <param name="key">Key để xóa cache</param>
        /// <returns></returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// Xóa tất cả cache theo pattern
        /// </summary>
        /// <param name="pattern">Pattern cần để xóa ví dụ ("warehouse:*")</param>
        /// <returns></returns>
        Task RemovePatternAsync(string pattern);

        /// <summary>
        /// Kiểm tra key có tồn tại không
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> ExistAsync(string key);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.RealTimeNoti
{
    public interface IRealtimeNotificationService
    {

        /// <summary>
        /// Gửi notification tới 1 user cụ thể
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendToUserAsync(string userId, string method, object message);

        /// <summary>
        /// Gửi notification tới tất cả user đang kết nối   
        /// </summary>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendToAllAsync(string method, object message);

        /// <summary>
        /// Gửi notification đến 1 group cụ thể
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="method"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendToGroupAsync(string groupName, string method, object message);

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.BackgroundJob
{
    public interface IBackgroundJobService
    {
        /// <summary>
        /// Dọn dẹp refresh tokens hết hạn
        /// Chạy hằng ngày lúc 2 giờ sáng
        /// </summary>
        /// <returns></returns>
        Task CleanUpExpiredRefreshTokenAsyncs();

        /// <summary>
        /// Gửi báo cáo tổng hợp hằng ngày
        /// Chạy hằng ngày lúc 8 giờ sáng
        /// </summary>
        /// <returns></returns>
        Task SendDailySummaryReportAsync();

    }
}

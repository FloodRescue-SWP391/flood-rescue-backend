using FloodRescue.Repositories.Entites;
using FloodRescue.Repositories.Interface;
using FloodRescue.Services.Implements.RescueRequest;
using FloodRescue.Services.Interface.BackgroundJob;
using FloodRescue.Services.Interface.Cache;
using FloodRescue.Services.Interface.RealTimeNoti;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
// Đặt Alias để tránh nhầm lẫn giữa Namespace/Service và Entity
using RescueRequestEntity = FloodRescue.Repositories.Entites.RescueRequest;
using InventoryEntity = FloodRescue.Repositories.Entites.Inventory;
using RefreshTokenEntity = FloodRescue.Repositories.Entites.RefreshToken;
namespace FloodRescue.Services.Implements.BackgroundJob
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IUnitOfWork _unitOfWork;

        private readonly ILogger<BackgroundJobService> _logger;

        private readonly ICacheService _cacheService;

        private readonly IRealtimeNotificationService _notificationService;

        public BackgroundJobService(IUnitOfWork unitOfWork, ILogger<BackgroundJobService> logger, ICacheService cacheService, IRealtimeNotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _cacheService = cacheService;
            _notificationService = notificationService;
        }

        public async Task CleanUpExpiredRefreshTokenAsyncs()
        {
            //đánh dấu thời gian bắt đầu của cron job
            var startTime = DateTime.UtcNow;

            _logger.LogInformation("[CRON] Clean Up Expired Refresh Token - Start at {StartTime}", startTime);

            try
            {
                //lấy các token cần xóa
                //query database để tìm các tokens được tính là không dùng được nữa
                // thời gian hết hạn - đã qua sử dụng - đã bị thu hồi

                IEnumerable<RefreshTokenEntity> expiredTokens = await _unitOfWork.RefreshTokens.GetAllAsync((RefreshTokenEntity rt) => rt.ExpiredAt < DateTime.UtcNow || rt.IsUsed || rt.IsRevoked);

                //Đếm token để ghi log
                int tokenCount = expiredTokens.Count();


                if (tokenCount == 0)
                {
                    _logger.LogInformation("[CRON] Clean Up Expired Refresh Token - No expired tokens found at {CheckTime}", DateTime.UtcNow);

                    return;
                }

                //thực hiện xóa các token
                foreach (var token in expiredTokens)
                {
                    _unitOfWork.RefreshTokens.Delete(token);
                }


                int deletedCount = await _unitOfWork.SaveChangesAsync();

                var excutionTime = DateTime.UtcNow - startTime;


                _logger.LogInformation("[CRON] Clean Up Expired Refresh Token - DONE - Deleted {Deleted}/{TokenCount} refresh tokens at {ExcutionTime}ms", deletedCount, tokenCount, excutionTime.TotalMilliseconds);


            }
            catch (Exception ex)
            {
                _logger.LogError("[CRON] Clean Up Expired Refresh Token - Failed at {FailedTime} - Error: {Error}", DateTime.UtcNow, ex.Message);

                throw;
            }
        }

        public async Task SendDailySummaryReportAsync()
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("[CRON] Send Daily Summary Report - Start at {StartTime}", startTime);

            try
            {
                var yesterday = DateTime.UtcNow.AddDays(-1);

                //Thống kê các rescue request mới trong vòng 24 giờ qua
                //IEnumerable<RescueRequestService> newRescueRequests = await _unitOfWork.RescueRequests.GetAllAsync((RescueRequestService rr) => rr.CreatedTime >= yesterday);
                IEnumerable<RescueRequestEntity> newRescueRequests = await _unitOfWork.RescueRequests
                    .GetAllAsync((RescueRequestEntity rr) => rr.CreatedTime >= yesterday);

                int newRescueRequestCount = newRescueRequests.Count();

                //Kiểm tra các inventory có quantity thấp dưới 10
                IEnumerable<InventoryEntity> lowInventoryItems = await _unitOfWork.Inventories
                    .GetAllAsync((InventoryEntity inv) => inv.Quantity < 10);

                int lowInventoryCount = lowInventoryItems.Count();

                var report = new
                {
                    ReportDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    GeneratedAt = DateTime.UtcNow,
                    Statistics = new
                    {
                        NewRescueRequest = newRescueRequestCount,
                        LowInventoryCount = lowInventoryCount
                    },
                };

                var alerts = new List<string>();

                if (lowInventoryCount > 0)
                {
                    alerts.Add($"There are {lowInventoryCount} low inventories need more supplies");
                }

                if (newRescueRequestCount > 50)
                {
                    alerts.Add($"High volume of new rescue requests received in the last 24 hours with {newRescueRequestCount} rescue requests");
                }


                //gửi thông báo về cho admin
                await _notificationService.SendToGroupAsync("Admin", "DailySummaryReport", new
                {
                    Report = report,
                    Alerts = alerts,
                    Timestamp = DateTime.UtcNow

                });

                var excutionTime = DateTime.UtcNow - startTime;

                _logger.LogInformation("[CRON] Send Daily Summary Report - DONE at {ExcutionTime}ms", excutionTime.TotalMilliseconds);

            }
            catch (Exception ex)
            {
                _logger.LogError("[CRON] Send Daily Summary Report - Failed at {FailedTime} - Error: {Error}", DateTime.UtcNow, ex.Message);
                throw;
            }
        }
   
    }
}

using FloodRescue.Services.Interface.RealTimeNoti;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using FloodRescue.Services.Hubs;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace FloodRescue.Services.Implements.RealTimeNoti
{
    public class RealtimeNotificationService : IRealtimeNotificationService
    {

        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<RealtimeNotificationService> _logger;

        public RealtimeNotificationService(IHubContext<NotificationHub> hubContext, ILogger<RealtimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendToAllAsync(string method, object message)
        {
            _logger.LogInformation("[RealtimeNotificationService - SignalR] Broadcasting to all clients. Method: {Method}", method);
            await _hubContext.Clients.All.SendAsync(method, message);   
        }

        public async Task SendToGroupAsync(string groupName, string method, object message)
        {
            _logger.LogInformation("[RealtimeNotificationService - SignalR] Sending to group '{GroupName}'. Method: {Method}", groupName, method);
            await _hubContext.Clients.Group(groupName).SendAsync(method, message);
        }

        public async Task SendToUserAsync(string userId, string method, object message)
        {
            _logger.LogInformation("[RealtimeNotificationService - SignalR] Sending to user '{UserId}'. Method: {Method}", userId, method);
            await _hubContext.Clients.User(userId).SendAsync(method, message);
        }
    }
}

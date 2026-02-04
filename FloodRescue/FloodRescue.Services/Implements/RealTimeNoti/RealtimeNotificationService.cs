using FloodRescue.Services.Interface.RealTimeNoti;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using FloodRescue.Services.Hubs;
using System.Runtime.InteropServices;

namespace FloodRescue.Services.Implements.RealTimeNoti
{
    public class RealtimeNotificationService : IRealtimeNotificationService
    {

        private readonly IHubContext<NotificationHub> _hubContext;

        public RealtimeNotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendToAllAsync(string method, object message)
        {
            await _hubContext.Clients.All.SendAsync(method, message);   
        }

        public async Task SendToGroupAsync(string groupName, string method, object message)
        {
            await _hubContext.Clients.Group(groupName).SendAsync(method, message);
        }

        public async Task SendToUserAsync(string userId, string method, object message)
        {
            await _hubContext.Clients.User(userId).SendAsync(method, message);
        }
    }
}

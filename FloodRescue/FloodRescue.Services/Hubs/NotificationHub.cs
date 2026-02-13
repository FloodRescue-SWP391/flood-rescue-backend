using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace FloodRescue.Services.Hubs
{
    public class NotificationHub: Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// chạy qua hàm này khi có kết nối từ client
        /// </summary>
        /// <returns></returns>
      public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            _logger.LogInformation("[NotificationHub - SignalR] Client connected. ConnectionId: {UserId}", user);

            //add user vào group sau
            //......
            //phần code để add
            
            if (user?.Identity?.IsAuthenticated == true)
            {
                var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value);

                foreach (var roleName in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, roleName);

                    _logger.LogInformation("[NotificationHub - SignalR] Connection {UserId} added to role group {GroupName}", Context.ConnectionId, roleName);
                }
            }

            
            //------------

            await base.OnConnectedAsync();
        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("[NotificationHub - SignalR] Client disconnected. ConnectionId: {UserId}", Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// này để client gọi vào
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);  
            _logger.LogInformation("[NotificationHub - SignalR] Connection {UserId} joined group {GroupName}", Context.ConnectionId, groupName);

            
        }


        public async Task JoinTeamGroup(string teamID)
        {
            if (string.IsNullOrWhiteSpace(teamID))
            {
                _logger.LogWarning("[NotificationHub - SignalR] JoinTeamGroup called with empty teamID. ConnectionId: {ConnectionId}", Context.ConnectionId);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, teamID);

            _logger.LogInformation("[NotificationHub - SignalR] Connection {UserId} joined team group {TeamID}", Context.ConnectionId, teamID);

            await Clients.Caller.SendAsync("JoinedTeamGroup", new { 
                TeamID = teamID,
                Message = $"You have successfully joined the team group {teamID}."
            });

        }

        /// <summary>
        /// Client gọi method này để rời khỏi room/group
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("[NotificationHub - SignalR] Connection {UserId} left group {Group}", Context.ConnectionId, groupName);
        }
    }
}

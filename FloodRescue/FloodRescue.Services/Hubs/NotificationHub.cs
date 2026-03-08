using Confluent.Kafka;
using FloodRescue.Repositories.Entites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
            _logger.LogInformation(
               "[NotificationHub] Client connected. ConnectionId: {ConnectionId}",
               Context.ConnectionId);

            var user = Context.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning(
                  "[NotificationHub] Unauthenticated connection detected. ConnectionId: {ConnectionId}",
                  Context.ConnectionId);
                await base.OnConnectedAsync();
                return;
            }

            var userID = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value; // Cố gắng lấy userID từ claim "NameIdentifier" hoặc "sub"

            List<string> roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var teamID = user.FindFirst("TeamID")?.Value; // Lấy teamID nếu có
            var isLeader = user.FindFirst("IsLeader")?.Value; // Lấy thông tin leader nếu có    

            // 1 ConnectionID đến từ màn hình của Coordinator
            // Coordinator đã lấy role trong đó kiểm tra là Rescue Coordinator
            // roles lúc này là new List<string> { "Rescue Coordinator" };
            // 1. Auto-join vào role group
            // Role claim = "Rescue Coordinator" → join group "Rescue Coordinator"
            // GroupSettings constants cũng = "Rescue Coordinator" → handler gửi đúng group
            foreach (var roleName in roles)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roleName);

                _logger.LogInformation(
                    "[NotificationHub] Connection {ConnectionId} added to role group '{RoleGroup}'",
                    Context.ConnectionId, roleName);
            }

            // 2. Nếu là Rescue Team Member → auto-join vào team group
            // DispatchMissionKafkaHandler gửi đến "Team_{RescueTeamID}_Leader"
            // IncidentResolvedHandler gửi đến "Team_{RescueTeamID}_Leader"
            if (!string.IsNullOrWhiteSpace(teamID))
            {
                var teamGroup = $"Team_{teamID}";
                await Groups.AddToGroupAsync(Context.ConnectionId, teamGroup);

                _logger.LogInformation(
                    "[NotificationHub] Connection {ConnectionId} added to team group '{TeamGroup}'",
                    Context.ConnectionId, teamGroup);

                // 3. Nếu là Leader → thêm vào leader group
                if (string.Equals(isLeader, "True", StringComparison.OrdinalIgnoreCase))
                {
                    var leaderGroupName = $"Team_{teamID}_Leader";
                    await Groups.AddToGroupAsync(Context.ConnectionId, leaderGroupName);

                    _logger.LogInformation(
                        "[NotificationHub] Leader detected. Connection {ConnectionId} added to group '{LeaderGroup}'",
                        Context.ConnectionId, leaderGroupName);
                }
            }

            _logger.LogInformation(
                "[NotificationHub] User setup complete. UserId={UserId}, Roles=[{Roles}], TeamID={TeamID}, IsLeader={IsLeader}",
                userID,
                string.Join(", ", roles),
                teamID ?? "N/A",
                isLeader ?? "N/A");

            await base.OnConnectedAsync();

        }


        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
             "[NotificationHub] Client disconnected. ConnectionId: {ConnectionId}",
             Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}

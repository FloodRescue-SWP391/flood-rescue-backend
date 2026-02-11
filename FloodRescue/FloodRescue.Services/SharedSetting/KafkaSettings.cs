using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.SharedSetting
{
    public static class KafkaSettings
    {
        // tạo tên mẫu trước cho 1 số topic để dùng sau
        public const string RESCUE_REQUEST_TOPIC = "rescue-request-topic";
        public const string RESCUE_REQUEST_CREATED_TOPIC = "rescue-request-created-topic";
        public const string RESCUE_REQUEST_ACCEPTED_TOPIC = "rescue-request-accepted-topic";
        public const string RESCUE_REQUEST_REJECTED_TOPIC = "rescue-request-rejected-topic";
        public const string NOTIFICATION_TOPIC = "notification-topic";   
    }

    public static class Groups
    {
        public const string RESCUE_COORDINATOR_GROUP = "RescueCoordinator";
        public const string RESCUE_TEAM_GROUP = "RescueTeam";
    }
}

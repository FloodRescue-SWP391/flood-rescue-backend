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
        public const string RESCUE_REQUEST_TOPIC= "rescue-request-topic";
        public const string NOTIFICATION_TOPIC = "notification-topic";

        //topic mới dành cho Coordinator Dispatch - khi assign mission mới cho team
        public const string MISSION_ASSIGN_TOPIC = "mission-assigned-topic";

        public const string TEAM_ACCEPTED_TOPIC = "team-accepted-topic";

        public const string TEAM_REJECTED_TOPIC = "team-rejected-topic";    
    }
}

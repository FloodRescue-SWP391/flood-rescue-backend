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
        public const string RescueRequestTopic = "rescue-request-topic";
        public const string NotificationTopic = "notification-topic";   
    }
}

using FloodRescue.Services.DTO.Request.RescueRequest;
using FloodRescue.Services.Interface.Email;
using FloodRescue.Services.Interface.Kafka;
using FloodRescue.Services.Interface.RealTimeNoti;
using FloodRescue.Services.SharedSetting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Kafka
{
    public class RescueRequestKafkaHandler : IKafkaHandler
    {
        private readonly ILogger<RescueRequestKafkaHandler> _logger;
        private readonly IRealtimeNotificationService _notificationService;
        private readonly Lazy<IGmailService> _gmailService;   
        //private readonly ISmsService _smsService;

        public RescueRequestKafkaHandler(ILogger<RescueRequestKafkaHandler> logger, IRealtimeNotificationService notificationService, Lazy<IGmailService> gmailService)
        {
            _logger = logger;
            _notificationService = notificationService;
            _gmailService = gmailService;
            //_smsService = smsService;
        }
        // Topic lấy từ KafkaSettings đã tạo sẵn trong SharedSetting
        public string Topic => KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[RescueRequestKafkaHandler - Kafka Consumer] Received message on topic: {Topic}", Topic);

            try
            {
                // 1. Parse message JSON thành object DTO
                var kafkaMessage = JsonSerializer.Deserialize<RescueRequestKafkaMessage>(message);

                if (kafkaMessage == null || kafkaMessage.RescueRequestID == Guid.Empty)
                {
                    _logger.LogWarning("[RescueRequestKafkaHandler - Kafka Consumer] Failed to deserialize message from topic {Topic}. Message skipped.", Topic);
                    return;
                }

                _logger.LogInformation("[RescueRequestKafkaHandler - Kafka Consumer] Processing RescueRequest ID: {Id}, ShortCode: {ShortCode}, Phone: {Phone}",
                    kafkaMessage.RescueRequestID, kafkaMessage.ShortCode, kafkaMessage.CitizenPhone);

                // 2. Gửi SMS thông báo cho citizen (PLACEHOLDER - tích hợp Twilio/Vonage sau)
                await SendGmailNotificationAsync(kafkaMessage);

                _logger.LogInformation("[RescueRequestKafkaHandler - Kafka Consumer] Send Rescue Request ID {ID} with Short Code {ShortCode} to Citizen Phone {Citizen} sucessfully",
                   kafkaMessage.RescueRequestID, kafkaMessage.ShortCode, kafkaMessage.CitizenPhone);

                // 3. Gửi realtime notification cho Coordinator qua SignalR (dùng IRealtimeNotificationService đã có)
                await _notificationService.SendToGroupAsync(
                    GroupSettings.RESCUE_COORDINATOR_GROUP,
                    "NewRescueRequest",
                    new
                    {
                        kafkaMessage.CitizenEmail,
                        kafkaMessage.RescueRequestID,
                        kafkaMessage.ShortCode,
                        kafkaMessage.RequestType,
                        kafkaMessage.CitizenPhone,
                        kafkaMessage.LocationLatitude,
                        kafkaMessage.LocationLongitude,
                        kafkaMessage.CreatedTime,
                        Message = $"New {kafkaMessage.RequestType} request received - ShortCode: {kafkaMessage.ShortCode}"
                    }
                );
                _logger.LogInformation("[RescueRequestKafkaHandler - SignalR] Sent NewRescueRequest notification to Coordinator group for ShortCode: {ShortCode}",
                    kafkaMessage.ShortCode);

                _logger.LogInformation("[RescueRequestKafkaHandler - Kafka Consumer] Successfully processed RescueRequest ID: {Id}", kafkaMessage.RescueRequestID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[RescueRequestKafkaHandler - Error] Failed to deserialize JSON message on topic: {Topic}", Topic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RescueRequestKafkaHandler - Error] Unexpected error processing message on topic: {Topic}", Topic);
            }
        }

        /// <summary>
        /// Placeholder cho SMS service - sau này inject ISmsService vào để gửi thật
        /// Hiện tại chỉ ghi log
        /// </summary>
        private async Task SendGmailNotificationAsync(RescueRequestKafkaMessage kafkaMessage)
        {
            if (string.IsNullOrEmpty(kafkaMessage.CitizenEmail))
            {
                _logger.LogWarning("[RescueRequestKafkaHandler - Email] No email provided for ShortCode: {ShortCode}. Skipping email notification.",
                    kafkaMessage.ShortCode);
                return;
            }

            try
            {
                string subject = $"[FloodRescue] Yêu cầu cứu hộ #{kafkaMessage.ShortCode} đã được tiếp nhận";

                // Rút gọn thành chuỗi text bình thường, có dấu đầy đủ
                string body = $@"Chào bạn,
Yêu cầu cứu hộ của bạn đã được hệ thống tiếp nhận thành công
Thông tin chi tiết:
- Mã tra cứu của bạn là: {kafkaMessage.ShortCode}.
- Số người cần hỗ trợ là: {kafkaMessage.PeopleCount}.
Chúng tôi sẽ liên hệ với bạn qua số điện thoại {kafkaMessage.CitizenPhone} trong thời gian sớm nhất.
Vui lòng giữ an toàn!
Trân trọng!";

                bool result = await _gmailService.Value.SendGmailAsync(kafkaMessage.CitizenEmail, subject, body);

                if (result)
                {
                    _logger.LogInformation("[RescueRequestKafkaHandler - Email] Email sent successfully to {Email} for ShortCode: {ShortCode}",
                        kafkaMessage.CitizenEmail, kafkaMessage.ShortCode);
                }
                else
                {
                    _logger.LogWarning("[RescueRequestKafkaHandler - Email] Email failed to send to {Email} for ShortCode: {ShortCode}. Continuing with SignalR notification.",
                        kafkaMessage.CitizenEmail, kafkaMessage.ShortCode);
                }

            }
            catch(Exception ex)
            {
                // Email fail không được crash Consumer — phải chạy tiếp để gửi SignalR ở bước 3
                _logger.LogError(ex, "[RescueRequestKafkaHandler - Email Error] Failed to send email to {Email}. Consumer will continue processing.",
                    kafkaMessage.CitizenEmail);

            }
          
        }

    }
}

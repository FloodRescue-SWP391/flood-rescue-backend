using FloodRescue.Services.DTO.Request.RescueRequest;
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
        public RescueRequestKafkaHandler(ILogger<RescueRequestKafkaHandler> logger, IRealtimeNotificationService notificationService)
        {
            _logger = logger;
            _notificationService = notificationService;
        }
        // Topic lấy từ KafkaSettings đã tạo sẵn trong SharedSetting
        public string Topic => KafkaSettings.RESCUE_REQUEST_CREATED_TOPIC;

        public async Task HandleAsync(string message)
        {
            _logger.LogInformation("[KafkaHandler] Received message on topic: {Topic}", Topic);

            try
            {
                // 1. Parse message JSON thành object DTO
                var kafkaMessage = JsonSerializer.Deserialize<RescueRequestKafkaMessage>(message);

                if (kafkaMessage == null)
                {
                    _logger.LogWarning("[KafkaHandler] Failed to deserialize message: {Message}", message);
                    return;
                }

                _logger.LogInformation("[KafkaHandler] Processing RescueRequest ID: {Id}, ShortCode: {ShortCode}, Phone: {Phone}",
                    kafkaMessage.RescueRequestID, kafkaMessage.ShortCode, kafkaMessage.CitizenPhone);

                // 2. Gửi SMS thông báo cho citizen (PLACEHOLDER - tích hợp Twilio/Vonage sau)
                await SendSmsNotificationAsync(kafkaMessage);

                // 3. Gửi realtime notification cho Coordinator qua SignalR (dùng IRealtimeNotificationService đã có)
                await _notificationService.SendToGroupAsync(
                    Groups.RESCUE_COORDINATOR_GROUP,
                    "NewRescueRequest",
                    new
                    {
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
                _logger.LogInformation("[KafkaHandler] Sent realtime notification to Rescue Coordinator group for ShortCode: {ShortCode}",
                    kafkaMessage.ShortCode);

                _logger.LogInformation("[KafkaHandler] Successfully processed RescueRequest ID: {Id}", kafkaMessage.RescueRequestID);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "[KafkaHandler] JSON deserialization error for message: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KafkaHandler] Unexpected error processing message on topic: {Topic}", Topic);
            }
        }

        /// <summary>
        /// Placeholder cho SMS service - sau này inject ISmsService vào để gửi thật
        /// Hiện tại chỉ ghi log
        /// </summary>
        private Task SendSmsNotificationAsync(RescueRequestKafkaMessage kafkaMessage)
        {
            // TODO: Tích hợp SMS service (Twilio, Vonage, SpeedSMS, ...)
            // Ví dụ: await _smsService.SendAsync(kafkaMessage.CitizenPhone, smsContent);

            string smsContent = $"[FloodRescue] Yêu cầu cứu hộ của bạn đã được tiếp nhận. " +
                                $"Mã theo dõi: {kafkaMessage.ShortCode}. " +
                                $"Loại: {kafkaMessage.RequestType}. " +
                                $"Trạng thái: Pending. " +
                                $"Chúng tôi sẽ liên hệ sớm nhất.";

            _logger.LogInformation("[SMS-PLACEHOLDER] Sending SMS to {Phone}: {Content}",
                kafkaMessage.CitizenPhone, smsContent);

            return Task.CompletedTask;
        }

    }
}

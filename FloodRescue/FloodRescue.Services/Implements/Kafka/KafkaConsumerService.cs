using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using FloodRescue.Services.Interface.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace FloodRescue.Services.Implements.Kafka
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly ILogger<KafkaConsumerService> _logger;

        // phải đồng nhất với producer gửi
        private readonly IConsumer<string, string> _consumer;

        //lấy toàn bộ tất cả các service vào để linh hoạt dùng - đưa nguyên kho service cho consumer xài
        private readonly IServiceProvider _serviceProvider;

        //hardcode trước mốt sửa
        private readonly List<string> _topics;

        public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            ConsumerConfig config = new ConsumerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = "FloodRescue-Consumer-Group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false   
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();

            // xài scope vì IKafkaHandler và các class implement đều là scope
            using (var scope = _serviceProvider.CreateScope())
            {
                var handlers = scope.ServiceProvider.GetServices<IKafkaHandler>();

                _topics = handlers.Select(h => h.Topic).Distinct().ToList();    
            }

            if (_topics.Any())
            {
                _logger.LogInformation("[KafkaConsumerService - Kafka Consumer] Topics discovered from handlers: {Topics}", string.Join(", ", _topics));
            }
            else
            {
                _logger.LogWarning("[KafkaConsumerService - Kafka Consumer] No topics found from any IKafkaHandler implementations. Consumer will have nothing to subscribe.");  
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_topics == null || _topics.Count == 0)
            {
                _logger.LogError("[KafkaConsumerService - Kafka Consumer] No topics available for subscribing. Consumer stopped.");
                return;
            }

            _consumer.Subscribe(_topics);
            _logger.LogInformation("[KafkaConsumerService - Kafka Consumer] Consumer started. Subscribed to: {Topic}", string.Join(", ", _topics));

            //lặp đến khi nào server tắt và không còn request nào nữa
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // biến này sẽ đi lấy dữ liệu từ kafka producer ở trên controller đăng kí
                    var result = _consumer.Consume(stoppingToken);


                    //ghi log theo dõi
                    _logger.LogInformation("[KafkaConsumerService - Kafka Consumer] Received message from topic {Topic}: Key={Key}, Value={Value}", result.Topic, result.Message.Key, result.Message.Value);

                    // xử lí message nhận được các topic 
                    // value lúc này đã chuyển thành json nên để string nhưng thực chất là object dto
                    await ProcessMessageAsync(result.Topic, result.Message.Key, result.Message.Value);


                    //commit lại sau khi xử lí thành công - commit thủ công để chắc chắn
                    _consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "[KafkaConsumerService - Error] Kafka consume failed. Reason: {Error}. Retrying in 5s.", ex.Error.Reason);
                    await Task.Delay(5000, stoppingToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "[KafkaConsumerService - Error] Handler threw exception while processing message: {Error}. Retrying in 2s.", ex.Message);

                    await Task.Delay(2000, stoppingToken);
                }
            }

        }

        /// <summary>
        /// Hàm xử lí chính thức của tất cả service phải đi qua kafka hàm này chạy ngầm nên cách gọi cũng sẽ khác
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private async Task ProcessMessageAsync (string topic, string key, string value)
        {
            using var scope = _serviceProvider.CreateScope();

            //lấy tất cả các service đã đăng kí trong kho ra
            //cụ thể là các service đã implement với IKafkaHandler
            IEnumerable<IKafkaHandler> kafkaServiceImplements = scope.ServiceProvider.GetServices<IKafkaHandler>();

            //tìm xem trong đống services đó cái nào có properties Topic trùng với topic truyền vào từ kafka producer
            IKafkaHandler? handler = kafkaServiceImplements.FirstOrDefault((service) => service.Topic == topic);

            if (handler != null)
            {
                _logger.LogInformation("[KafkaConsumerService - Kafka Consumer] Routing message to handler for topic {Topic}", handler.Topic);

                //gọi hàm xử lí service của request đó - thay vì gọi service trong controller thì gọi ở đây
                //nhưng vẫn phải viết service như bình thường
                await handler.HandleAsync(value);
            }
            else
            {
                _logger.LogWarning("[KafkaConsumerService - Kafka Consumer] No handler registered for topic {Topic}. Message discarded.", topic);
            }
        }
    }
}

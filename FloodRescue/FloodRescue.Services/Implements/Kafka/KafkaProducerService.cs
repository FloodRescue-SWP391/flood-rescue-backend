using Confluent.Kafka;
using FloodRescue.Services.Interface.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FloodRescue.Services.Implements.Kafka
{
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducerService> _logger;

        public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
        {
            _logger = logger;

            ProducerConfig config = new ProducerConfig
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                ClientId = "Floodrescue-Producer",
                Acks = Acks.All,
                EnableIdempotence = true

            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public void Dispose()
        {
            //block lại trước để cho thêm 10 giây để gửi thêm message
            _producer?.Flush(TimeSpan.FromSeconds(10));

            //-- sau 10 giây đó sẽ dispose hết toàn bộ
            _producer?.Dispose();   
        }

        public async Task ProduceAsync<T>(string topic, string key, T message)
        {
            try
            {
                //biến dữ liệu thành json rồi mới gửi đi
                var jsonMessage = JsonSerializer.Serialize(message);

                //gửi lên server kafka
                var result = await _producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = key,
                    Value = jsonMessage

                });


                //Bên trong kafka có nhiều topic
                //Bên topic có nhiều partition
                //Bên trong partition có nhiều offset (bản ghi) 
                _logger.LogInformation("Kafka message sent to {Topic} partition {Partition} offset {Offset}", result.Topic, result.Partition.Value, result.Offset.Value);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Error producing Kafka message to {Topic} with {Reason}", topic, ex.Error.Reason);
                throw;
            }
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Kafka
{
    public interface IKafkaProducerService
    {
        /// <summary>
        /// Gửi message lên Kafka topic, có topic hồi consumer mới đi xử lí
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="topic">Tên topic</param>
        /// <param name="key">Key của message (dùng để partition)</param>
        /// <param name="message">Nội dung message</param>
        /// <returns></returns>
        Task ProduceAsync<T>(string topic, string key, T message);
    }
}

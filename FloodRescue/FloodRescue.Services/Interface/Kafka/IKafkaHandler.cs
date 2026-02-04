using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.Interface.Kafka
{
    public interface IKafkaHandler
    {
        string Topic { get; }

        /// <summary>
        /// Hàm xử lí chính, khai báo toàn bộ mọi phương thức xử lí service này vào đây
        /// Cho những request xài các service liên quan đến kafka
        /// Parse message ra thành dạng object rồi mới xử lí các logic bên trong
        /// Chỉ gọi các hàm từ _services đã khai báo trước đó bên các class service khác
        /// Cái này chỉ là nơi để gọi các hàm service khác thôi, không được phép thao tác trực tiếp với database ở đây  
        /// </summary>
        /// <param name="message">nhận vào message là các request dto đã được đóng gói</param>
        /// <returns></returns>
        Task HandleAsync(string message);
    }
}

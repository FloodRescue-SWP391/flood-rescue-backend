using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueRequest
{
    public class RescueRequestFilterDTO
    {
        public List<string>? Status { get; set; }
        public string? RequestType { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Số trang hiện tại (bắt đầu từ 1)
        /// Frontend gửi lên khi user click sang trang tiếp theo
        /// Default = 1 -> lần đầu tiên không cần truyền
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Số bản ghi mỗi trang 
        /// Default = 10 -> Mỗi trang hiển thị 10 đơn
        /// Frontend có thể cho user chọn: 10, 25, 50
        /// </summary>
        public int PageSize { get; set; } = 10;

    }
}

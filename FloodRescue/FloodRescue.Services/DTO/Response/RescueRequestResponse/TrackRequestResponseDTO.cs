using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class TrackRequestResponseDTO
    {
        public Guid RescueRequestID { get; set; }
        public string ShortCode { get; set; } = string.Empty;

        /// <summary>
        /// Tên đã được masking: "Nguyễn Văn A" → "Nguyễn V*** A"
        /// </summary>
        public string CitizenName { get; set; } = string.Empty;

        /// <summary>
        /// SĐT đã được masking: "0901234567" → "090****567"
        /// </summary>
        public string CitizenPhone { get; set; } = string.Empty;
    
        public string RequestType { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái Request: Pending, Processing, Completed, Rejected
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Lý do từ chối (nếu Status = Rejected)
        /// </summary>
        public string? RejectedNote { get; set; }

        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Trạng thái nhiệm vụ cứu hộ (Assigned, InProgress, Completed)
        /// Null nếu chưa được dispatch
        /// </summary>
        public string? MissionStatus { get; set; }

        /// <summary>
        /// Tên đội cứu hộ đang xử lý (nếu có)
        /// </summary>
        public string? TeamName { get; set; }

        public int PeopleCount {get; set;}

        /// <summary>
        /// Thông tin đội trưởng (nếu có mission)
        /// </summary>
        public TeamMemberDTO? TeamLeader { get; set; }

        /// <summary>
        /// Danh sách thành viên đội cứu hộ (nếu có mission)
        /// </summary>
        public List<TeamMemberDTO>? Members { get; set; }
    }
}

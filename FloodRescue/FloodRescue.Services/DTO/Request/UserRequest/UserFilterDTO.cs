namespace FloodRescue.Services.DTO.Request.UserRequest
{
    public class UserFilterDTO
    {
        /// <summary>
        /// Tìm kiếm theo FullName hoặc Phone
        /// </summary>
        public string? SearchKeyword { get; set; }

        /// <summary>
        /// Lọc theo RoleID (RC, IM, RT)
        /// </summary>
        public string? RoleID { get; set; }

        /// <summary>
        /// Lọc theo trạng thái: true = đang hoạt động, false = đã bị khóa
        /// </summary>
        public bool? IsActive { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}

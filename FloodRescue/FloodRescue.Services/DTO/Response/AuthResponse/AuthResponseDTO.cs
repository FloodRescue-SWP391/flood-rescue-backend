using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Response.AuthResponse
{
    //thông tin trả về
    public class AuthResponseDTO
    {
        //Trả ra access token mới
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }   //thời gian hết hạn tính bằng giây - 900 giây  = 15 phút

        //Thông tin user đi kèm với cái token đó
        public Guid UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

    }
}

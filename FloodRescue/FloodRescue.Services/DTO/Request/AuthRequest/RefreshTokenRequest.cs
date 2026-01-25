using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.AuthRequest
{
    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = ("Access Token is required"))]
        public string AccessToken { get; set; } = string.Empty;
    }
}

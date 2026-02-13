using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Services.DTO.Request.RescueRequest
{
    public class CreateRescueRequestDTO
    {
        [Required(ErrorMessage = "RequestType is required")]
        [MaxLength(20)]
        public string RequestType { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Latitude is required")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
        public double LocationLatitude { get; set; }

        [Required(ErrorMessage = "Longitude is required")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
        public double LocationLongitude { get; set; }
        [Required(ErrorMessage = "PhoneNumber is required")]
        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Invalid Vietnamese phone number")]
        [MaxLength(15)]
        public string PhoneNumber { get; set; } = string.Empty;

        public int PeopleCount { get; set; }

        /// <summary>
        /// Danh sách URL ảnh đã upload lên cloud (Cloudinary, S3, ...) trước đó
        /// </summary>
        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}

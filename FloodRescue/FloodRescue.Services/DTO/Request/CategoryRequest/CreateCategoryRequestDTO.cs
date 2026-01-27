using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.Category
{
    public class CreateCategoryRequestDTO
    {
        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;
    }
}

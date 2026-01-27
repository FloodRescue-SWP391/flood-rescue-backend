using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.ReliefItem
{
    public class CreateReliefItemRequestDTO
    {
        [Required]
        [MaxLength(100)]
        public string ReliefItemName { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;
    }
}

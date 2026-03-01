using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.ReliefOrderRequest
{
    public class PrepareOrderRequestDTO
    {
        [Required]
        public Guid ReliefOrderID { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one item is required")]
        public List<PrepareOrderItemDTO> Items { get; set; } = new();
    }
}

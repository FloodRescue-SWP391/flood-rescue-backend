using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.ReliefOrderRequest
{
    public class PrepareOrderItemDTO
    {
        [Required]
        public int ReliefItemID { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}

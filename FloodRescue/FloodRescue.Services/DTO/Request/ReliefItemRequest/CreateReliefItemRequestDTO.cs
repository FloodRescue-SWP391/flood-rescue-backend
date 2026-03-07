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
        public int UnitID { get; set; }
    }
}

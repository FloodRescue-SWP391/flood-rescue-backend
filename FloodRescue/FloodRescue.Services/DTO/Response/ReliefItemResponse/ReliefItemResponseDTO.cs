namespace FloodRescue.Services.DTO.Response.ReliefItem
{
    public class ReliefItemResponseDTO
    {
        public int ReliefItemID { get; set; }
        public string ReliefItemName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}

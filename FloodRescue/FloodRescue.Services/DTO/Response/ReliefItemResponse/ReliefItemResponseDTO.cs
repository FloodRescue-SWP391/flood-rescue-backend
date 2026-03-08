namespace FloodRescue.Services.DTO.Response.ReliefItem
{
    public class ReliefItemResponseDTO
    {
        public int ReliefItemID { get; set; }
        public string ReliefItemName { get; set; } = string.Empty;
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = string.Empty;    
        public int UnitID { get; set; }
        public string UnitName { get; set; } = string.Empty;    
    }
}

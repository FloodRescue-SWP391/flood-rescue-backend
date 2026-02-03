namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class RescueRequestResponseDTO
    {
        public Guid RescueRequestID { get; set; }
        public string? CitizenName { get; set; }
        public string CitizenPhone { get; set; } = string.Empty;
        public string? Address { get; set; }
        public double LocationLatitude { get; set; }
        public double LocationLongitude { get; set; }
        public int PeopleCount { get; set; }
        public string? Description { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public string? RejectedNote { get; set; }
        public DateTime CreatedTime { get; set; }
        public Guid? CoordinatorID { get; set; }
        public string? CoordinatorBy { get; set; }
    }
}
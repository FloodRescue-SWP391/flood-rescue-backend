namespace FloodRescue.Services.DTO.Request.RescueRequestRequest
{
    public class RescueRequestRequestDTO
    {
        public string? CitizenName { get; set; }
        public string? CitizenPhone { get; set; }
        public string? Address { get; set; }
        public double? LocationLatitude { get; set; }
        public double? LocationLongitude { get; set; }
        public int? PeopleCount { get; set; }
        public string? Description { get; set; }
        public string? RequestType { get; set; }
        public string? Status { get; set; }
        public string? RejectedNote { get; set; }
        public Guid? CoordinatorID { get; set; }
    }
}
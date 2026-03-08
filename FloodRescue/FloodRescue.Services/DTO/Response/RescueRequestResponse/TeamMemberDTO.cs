namespace FloodRescue.Services.DTO.Response.RescueRequestResponse
{
    public class TeamMemberDTO
    {
        public Guid UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsLeader { get; set; }
    }
}
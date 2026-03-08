using System.ComponentModel.DataAnnotations;

namespace FloodRescue.Services.DTO.Request.UserRequest
{
    public class UpdateUserRequestDTO
    {
        [MaxLength(100, ErrorMessage = "FullName must not exceed 100 characters")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Phone format is invalid")]
        [MaxLength(15, ErrorMessage = "Phone must not exceed 15 characters")]
        public string? Phone { get; set; }

        [RegularExpression("^(RC|IM|RT)$", ErrorMessage = "RoleID must be RC (Rescue Coordinator), IM (Inventory Manager), or RT (Rescue Team)")]
        public string? RoleID { get; set; }
    }
}

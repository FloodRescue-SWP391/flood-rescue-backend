using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("Roles")]
    public class Role
    {
        [Column("RoleID", TypeName = "char(2)")]
        [Key]
        [Required(ErrorMessage = "RoleID cannot be blank")]
        [RegularExpression("^(AD|RC|IM|RT)$")]
        [MaxLength(2)]
        public required string RoleID { get; set; }

        [Column("RoleName", TypeName = "varchar(100)")]
        [Required(ErrorMessage = "RoleName cannot be blank")]
        [MaxLength(100)]
        public required string RoleName { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }

        //object navigation
        [JsonIgnore]
        public ICollection<User>? Users { get; set; }

    }
}

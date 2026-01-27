using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("Users")]
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Phone), IsUnique = true)]
    public class User
    {
        [Key]
        [Column("UserID", TypeName = "uniqueidentifier")]
        public Guid UserID { get; set; } = Guid.NewGuid();

        [Column("Username", TypeName = "nvarchar(50)")]
        [Required(ErrorMessage = "Username cannot be blank")]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;
        // Password hash - Fixed 64 bytes
        [Column("Password", TypeName = "varbinary(64)")]
        [Required(ErrorMessage = "Password cannot be blank")]
        [MinLength(8)]
        public byte[] Password { get; set; } = Array.Empty<byte>();
        // Salt - Fixed 128 bytes
        [Column("Salt", TypeName = "varbinary(128)")]
        [Required(ErrorMessage = "Salt cannot be blank")]
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        [Column("Phone", TypeName = "varchar(15)")]
        [Required(ErrorMessage = "Phone cannot be blank")]
        [MaxLength(15)]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        [Column("FullName", TypeName = "nvarchar(100)")]
        [Required(ErrorMessage = "FullName cannot be blank")]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;    

        [Column("RoleID", TypeName = "char(2)")]
        [Required(ErrorMessage = "RoleID cannot be blank")]
        [MaxLength(2)]
        public string RoleID { get; set; } = string.Empty;

        [ForeignKey(nameof(RoleID))]
        [JsonIgnore]
        public Role? Role { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; } = false;


    }
}

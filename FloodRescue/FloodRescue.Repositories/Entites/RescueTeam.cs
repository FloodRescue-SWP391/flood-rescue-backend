using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("RescueTeams")]
    public class RescueTeam
    {
        [Key]
        [Column("RescueTeamID", TypeName = "uniqueidentifier")]
        public Guid RescueTeamID { get; set; } = Guid.NewGuid();

        [Column("TeamName", TypeName = "nvarchar(100)")]
        [Required(ErrorMessage = "TeamName cannot be blank")]
        [MaxLength(100)]
        public string TeamName { get; set; } = string.Empty;

        [Column("City", TypeName = "nvarchar(255)")]
        [Required(ErrorMessage = "City cannot be blank")]
        [MaxLength(255)]
        public string City { get; set; } = string.Empty;

        [Column("CurrentStatus", TypeName = "varchar(20)")]
        [Required(ErrorMessage = "CurrentStatus cannot be blank")]
        [MaxLength(20)]
        public string CurrentStatus { get; set; } = string.Empty;

        [Column("CurrentLatitude", TypeName = "float")]
        public double CurrentLatitude { get; set; } 

        [Column("CurrentLongitude", TypeName = "float")]
        public double CurrentLongitude { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; } = false;

    }
}

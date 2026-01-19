using Microsoft.EntityFrameworkCore;
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
    [Table("RescueTeamMembers")]

    public class RescueTeamMember
    {
        [Column("UserID", TypeName = "uniqueidentifier")]
        [Key]
        public Guid UserID { get; set; } = new Guid();

        [ForeignKey(name: nameof(UserID))]
        [JsonIgnore]
        public User? User { get; set; }

        [Column("RescueTeamID", TypeName = "uniqueidentifier")]
        public Guid RescueTeamID { get; set; } = new Guid();

        [ForeignKey(name: nameof(RescueTeamID))]
        [JsonIgnore]
        public RescueTeam? RescueTeam { get; set; }

        [Column("IsLeader", TypeName = "BIT")]
        public bool IsLeader { get; set; }

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; }

    }
}

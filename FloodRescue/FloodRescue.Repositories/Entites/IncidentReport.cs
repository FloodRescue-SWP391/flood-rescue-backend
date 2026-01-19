using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
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
    [Table("IncidentReports")]
    public class IncidentReport
    {
        [Column("IncidentReportID", TypeName = "uniqueidentifier")]
        public Guid IncidentReportID { get; set; } = Guid.NewGuid();

        [Column("RescueMissionID", TypeName = "uniqueidentifier")]
        public Guid RescueMissionID { get; set; }

        [ForeignKey(nameof(RescueMissionID))]
        [JsonIgnore]
        public RescueMission? RescueMission { get; set; }

        [Column("ReportedID", TypeName = "uniqueidentifier")]
        public Guid ReportedID { get; set; }
        [ForeignKey(nameof(ReportedID))]
        [JsonIgnore]
        public User? Reported { get; set; }

        [Column("ResolvedBy", TypeName = "uniqueidentifier")]
        public Guid ResolvedBy { get; set; }
        [ForeignKey(nameof(ResolvedBy))]
        [JsonIgnore]
        public User? Resolver { get; set; }

        [Column("ResolvedTime", TypeName = "datetime2")]
        public DateTime? ResolvedTime { get; set; }

        [Column("Title", TypeName = "nvarchar(100)")]
        public string Title { get; set; } = string.Empty;

        [Column("Latitiude", TypeName = "float")]
        public double Latitiude { get; set; }

        [Column("Longitude", TypeName = "float")]
        public double Longitude { get; set; }

        [Column("CreatedTime", TypeName = "datetime2")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        [Column("Status", TypeName = "varchar(20)")]
        [Required]
        public string Status { get; set; } = string.Empty;

        [Column("Description", TypeName = "nvarchar(max)")]
        public string? Description { get; set; } = string.Empty;

        [Column("CoordinatorNote", TypeName = "nvarchar(max)")]
        public string? CoordinatorNote { get; set; } = string.Empty; 

    }
}

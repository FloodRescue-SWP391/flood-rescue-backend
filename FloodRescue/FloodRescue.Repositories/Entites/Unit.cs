using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("Units")]
    public class Unit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("UnitID", TypeName = "int")]
        public int UnitID { get; set; }

        [Column("UnitName", TypeName = "nvarchar(50)")]
        [Required]
        [MaxLength(50)]
        public string UnitName { get; set; } = string.Empty;

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; } = false;


    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FloodRescue.Repositories.Entites
{
    [Table("Categories")]
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("CategoryID", TypeName = "int")]
        public int CategoryID { get; set; }

        [Column("CategoryName", TypeName = "nvarchar(50)")]
        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [Column("IsDeleted", TypeName = "BIT")]
        public bool IsDeleted { get; set; } 
    }
}

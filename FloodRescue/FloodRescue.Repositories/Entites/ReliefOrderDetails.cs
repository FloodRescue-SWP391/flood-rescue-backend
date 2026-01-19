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
    [Table("ReliefOrderDetails")]
    public class ReliefOrderDetail
    {
        // 1. Primary Key
        [Key]
        [Column("ReliefOrderDetailID", TypeName = "uniqueidentifier")]
        public Guid ReliefOrderDetailID { get; set; } = Guid.NewGuid();

        // 2. Foreign Keys

        // Thuộc về Đơn hàng nào?
        [Required]
        [Column("ReliefOrderID", TypeName = "uniqueidentifier")]
        public Guid ReliefOrderID { get; set; }

        [ForeignKey(nameof(ReliefOrderID))]
        [JsonIgnore] // Ẩn để tránh loop khi load đơn hàng
        public ReliefOrder? ReliefOrder { get; set; }

        // Là món hàng gì? (Mì tôm, nước...)
        [Required]
        [Column("ReliefItemID", TypeName = "int")]
        public int ReliefItemID { get; set; }

        [ForeignKey(nameof(ReliefItemID))]
        public ReliefItem? ReliefItem { get; set; }

        // 3. Properties
        [Required]
        [Column("Quantity", TypeName = "int")]
        public int Quantity { get; set; }
    }
}

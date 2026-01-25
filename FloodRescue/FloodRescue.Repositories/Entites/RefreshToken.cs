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
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("RefresTokenID", TypeName = "int")]
        //Id tự động tăng
        public int RefresTokenID { get; set; }

        [Required]
        [Column("Token",TypeName = "varchar(500)")]
        //Chuỗi Token thật sự
        public string Token { get; set; } = string.Empty;

        [Required]
        [Column("JwtID", TypeName = "varchar(100)")]
        //Tham chiếu AccessToken nào ???
        public string JwtID { get; set; } = string.Empty;

        [Required]
        [Column("IsUsed", TypeName = "bit")]
        //Token đã dùng rồi không thể dùng lại nữa - ngược lại nếu chưa dùng thì có thể dùng để refresh
        public bool IsUsed { get; set; } = false;   

        [Required]
        [Column("IsRevoked", TypeName = "bit")]
        //Token đã bị thu hồi không thể dùng refresh nữa
        public bool IsRevoked { get; set; } = false;

        [Required]
        [Column("CreatedAt", TypeName = "datetime2")]
        //Thời điểm tạo refresh token
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("ExpiredAt", TypeName = "datetime2")]
        //Thời điểm hết hạn của refresh token
        public DateTime ExpiredAt { get; set; }

        //Foreign Key - Thuộc về User nào?
        [Required]
        [Column("UserID", TypeName = "uniqueidentifier")]
        public Guid UserID { get; set; }

        [ForeignKey(nameof(UserID))]
        [JsonIgnore]
        public User? User { get; set; } 
    }
}

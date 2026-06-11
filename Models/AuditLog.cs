using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("user_id")]
        public int? UserId { get; set; }

        [Required]
        [Column("username")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("action")]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Column("target")]
        [StringLength(100)]
        public string? Target { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Required]
        [Column("result")]
        [StringLength(20)]
        public string Result { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual Usuario? Usuario { get; set; }
    }
}

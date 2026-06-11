using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("role_permissions")]
    public class RolePermission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("rol_id")]
        public int RolId { get; set; }

        [Required]
        [Column("permission")]
        [StringLength(80)]
        public string Permission { get; set; } = string.Empty;

        [ForeignKey("RolId")]
        public virtual Rol? Rol { get; set; }
    }
}

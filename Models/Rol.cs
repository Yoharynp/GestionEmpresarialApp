using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("roles")]
    public class Rol
    {
        [Key]
        [Column("rol_id")]
        public int RolId { get; set; }

        [Required]
        [Column("rol_name")]
        [StringLength(50)]
        public string NombreRol { get; set; } = null!;

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [Column("username")]
        [StringLength(100, ErrorMessage = "El nombre de usuario no puede exceder los 100 caracteres")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [DataType(DataType.Password)]
        [Column("password")]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Column("rol_id")]
        public int RolId { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; } = null!;
    }
}

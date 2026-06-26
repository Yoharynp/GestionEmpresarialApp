using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("clients")]
    public class Client
    {
        [Key]
        [Column("client_id")]
        public int ClientId { get; set; }

        [Column("first_name")]
        [Required(ErrorMessage = "El nombre del cliente es requerido")]
        [StringLength(100, ErrorMessage = "El nombre del cliente no puede exceder los 100 caracteres")]
        public string FirstName { get; set; } = string.Empty;

        [Column("last_name")]
        [Required(ErrorMessage = "El apellido del cliente es requerido")]
        [StringLength(100, ErrorMessage = "El apellido del cliente no puede exceder los 100 caracteres")]
        public string LastName { get; set; } = string.Empty;

        [Column("phone")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Column("email")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Column("address")]
        [StringLength(255)]
        public string? Address { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "activo";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
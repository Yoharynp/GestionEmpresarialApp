using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GestionEmpresarialApp.Models
{
    [Table("products")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Column("code")]
        [Required(ErrorMessage = "El código del producto es requerido")]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Column("name")]
        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Column("price")]
        [Required(ErrorMessage = "El precio del producto es requerido")]
        [DataType(DataType.Currency)]
        [DefaultValue(0.0)]
        public decimal Price { get; set; }

        [Column("stock")]
        [Required(ErrorMessage = "El stock del producto es requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un número positivo")]
        public int Stock { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = "activo";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

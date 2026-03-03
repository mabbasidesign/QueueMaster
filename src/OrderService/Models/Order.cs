using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models;

public class Order
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "Unit price must be between 0.01 and 999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Created";

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

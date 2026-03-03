using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PaymentService.Models;

public class Payment
{
    [Key]
    public Guid TransactionId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "OrderId must be greater than 0")]
    public int OrderId { get; set; }

    [Required]
    [Range(0.01, 999999.99, ErrorMessage = "Amount must be between 0.01 and 999999.99")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "USD";

    [Required]
    [StringLength(50)]
    public string Method { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Processing";

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

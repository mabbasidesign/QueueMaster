using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public class ProcessedMessage
{
    [Key]
    [MaxLength(256)]
    public string MessageId { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}

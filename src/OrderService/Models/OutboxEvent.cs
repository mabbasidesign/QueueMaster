using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models;

public class OutboxEvent
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string Payload { get; set; } = string.Empty;
    
    public bool IsPublished { get; set; } = false;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? PublishedAt { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public string? LastError { get; set; }
}

namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Status { get; set; } = "Created";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

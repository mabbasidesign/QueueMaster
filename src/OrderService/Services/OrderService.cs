using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Events;
using System.Text.Json;

namespace OrderService.Services;

public class OrderService : IOrderService
{
    private readonly OrderDbContext _context;

    public OrderService(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        return await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        return await _context.Orders.AsNoTracking().ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Persist order first so the DB-generated Id is available in the event payload.
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderEvent = new OrderCreatedEvent(
            order.Id,
            order.CustomerName,
            order.ProductName,
            order.Quantity,
            order.UnitPrice,
            order.Quantity * order.UnitPrice,
            order.CreatedAtUtc
        );

        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "OrderCreated",
            Payload = JsonSerializer.Serialize(orderEvent),
            CreatedAt = DateTime.UtcNow,
            IsPublished = false,
            RetryCount = 0
        };

        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return order;
    }

    public async Task<Order> UpdateOrderAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
            return false;

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }
}

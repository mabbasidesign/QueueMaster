using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Services;
using OrderService.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register OrderService
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

// Register Service Bus
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection("ServiceBus"));
builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Order API Endpoints
app.MapGet("/api/orders", async (IOrderService orderService) =>
{
    var orders = await orderService.GetAllOrdersAsync();
    var response = orders.Select(o => new OrderResponse(
        o.Id,
        o.CustomerName,
        o.ProductName,
        o.Quantity,
        o.UnitPrice,
        o.Quantity * o.UnitPrice,
        o.Status,
        o.CreatedAtUtc));
    return Results.Ok(response);
})
.WithName("GetAllOrders")
.WithOpenApi();

app.MapGet("/api/orders/{id}", async (int id, IOrderService orderService) =>
{
    var order = await orderService.GetOrderByIdAsync(id);
    if (order is null)
        return Results.NotFound();
    
    var response = new OrderResponse(
        order.Id,
        order.CustomerName,
        order.ProductName,
        order.Quantity,
        order.UnitPrice,
        order.Quantity * order.UnitPrice,
        order.Status,
        order.CreatedAtUtc);
    return Results.Ok(response);
})
.WithName("GetOrderById")
.WithOpenApi();

app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService orderService, IServiceBusPublisher publisher) =>
{
    var order = new OrderService.Models.Order
    {
        CustomerName = request.CustomerName,
        ProductName = request.ProductName,
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        Status = "Pending",
        CreatedAtUtc = DateTime.UtcNow
    };
    
    var created = await orderService.CreateOrderAsync(order);
    var totalAmount = created.Quantity * created.UnitPrice;
    
    // Publish OrderCreated event to Service Bus
    await publisher.PublishOrderCreatedAsync(
        created.Id,
        created.CustomerName,
        created.ProductName,
        created.Quantity,
        created.UnitPrice,
        totalAmount,
        created.CreatedAtUtc);
    
    var response = new OrderResponse(
        created.Id,
        created.CustomerName,
        created.ProductName,
        created.Quantity,
        created.UnitPrice,
        totalAmount,
        created.Status,
        created.CreatedAtUtc);
    return Results.Created($"/api/orders/{response.Id}", response);
})
.WithName("CreateOrder")
.WithOpenApi();

app.MapPut("/api/orders/{id}", async (int id, UpdateOrderRequest request, IOrderService orderService) =>
{
    var existing = await orderService.GetOrderByIdAsync(id);
    if (existing is null)
        return Results.NotFound();
    
    existing.CustomerName = request.CustomerName;
    existing.ProductName = request.ProductName;
    existing.Quantity = request.Quantity;
    existing.UnitPrice = request.UnitPrice;
    existing.Status = request.Status;
    
    var updated = await orderService.UpdateOrderAsync(existing);
    var response = new OrderResponse(
        updated.Id,
        updated.CustomerName,
        updated.ProductName,
        updated.Quantity,
        updated.UnitPrice,
        updated.Quantity * updated.UnitPrice,
        updated.Status,
        updated.CreatedAtUtc);
    return Results.Ok(response);
})
.WithName("UpdateOrder")
.WithOpenApi();

app.MapDelete("/api/orders/{id}", async (int id, IOrderService orderService) =>
{
    var result = await orderService.DeleteOrderAsync(id);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteOrder")
.WithOpenApi();

app.Run();

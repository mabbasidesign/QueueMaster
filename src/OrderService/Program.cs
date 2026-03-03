using OrderService.Dtos;
using OrderService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var orders = new List<Order>();
var nextOrderId = 1;

var ordersApi = app.MapGroup("/api/v1/orders")
    .WithName("Orders")
    .WithOpenApi();

ordersApi.MapPost("/", CreateOrder)
    .WithName("CreateOrder")
    .WithOpenApi();

ordersApi.MapGet("/{id:int}", GetOrder)
    .WithName("GetOrder")
    .WithOpenApi();

ordersApi.MapGet("/", GetAllOrders)
    .WithName("GetAllOrders")
    .WithOpenApi();

ordersApi.MapPut("/{id:int}", UpdateOrder)
    .WithName("UpdateOrder")
    .WithOpenApi();

ordersApi.MapDelete("/{id:int}", DeleteOrder)
    .WithName("DeleteOrder")
    .WithOpenApi();

IResult CreateOrder(CreateOrderRequest request)
{
    var validationErrors = ValidateCreateOrder(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var order = new Order
    {
        Id = nextOrderId++,
        CustomerName = request.CustomerName.Trim(),
        ProductName = request.ProductName.Trim(),
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        Status = "Created"
    };

    orders.Add(order);

    return Results.Created($"/api/v1/orders/{order.Id}", ToResponse(order));
}

IResult GetOrder(int id)
{
    var order = orders.FirstOrDefault(item => item.Id == id);
    if (order is null)
    {
        return Results.NotFound(new { message = $"Order {id} not found" });
    }

    return Results.Ok(ToResponse(order));
}

IResult GetAllOrders()
{
    var response = orders.Select(ToResponse).ToList();
    return Results.Ok(response);
}

IResult UpdateOrder(int id, UpdateOrderRequest request)
{
    var validationErrors = ValidateUpdateOrder(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var order = orders.FirstOrDefault(item => item.Id == id);
    if (order is null)
    {
        return Results.NotFound(new { message = $"Order {id} not found" });
    }

    order.CustomerName = request.CustomerName.Trim();
    order.ProductName = request.ProductName.Trim();
    order.Quantity = request.Quantity;
    order.UnitPrice = request.UnitPrice;
    order.Status = request.Status.Trim();

    return Results.Ok(ToResponse(order));
}

IResult DeleteOrder(int id)
{
    var order = orders.FirstOrDefault(item => item.Id == id);
    if (order is null)
    {
        return Results.NotFound(new { message = $"Order {id} not found" });
    }

    orders.Remove(order);
    return Results.NoContent();
}

static OrderResponse ToResponse(Order order)
{
    return new OrderResponse(
        order.Id,
        order.CustomerName,
        order.ProductName,
        order.Quantity,
        order.UnitPrice,
        order.Quantity * order.UnitPrice,
        order.Status,
        order.CreatedAtUtc
    );
}

static Dictionary<string, string[]> ValidateCreateOrder(CreateOrderRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.CustomerName))
    {
        errors["customerName"] = ["Customer name is required."];
    }

    if (string.IsNullOrWhiteSpace(request.ProductName))
    {
        errors["productName"] = ["Product name is required."];
    }

    if (request.Quantity <= 0)
    {
        errors["quantity"] = ["Quantity must be greater than zero."];
    }

    if (request.UnitPrice <= 0)
    {
        errors["unitPrice"] = ["Unit price must be greater than zero."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateUpdateOrder(UpdateOrderRequest request)
{
    var errors = ValidateCreateOrder(new CreateOrderRequest(
        request.CustomerName,
        request.ProductName,
        request.Quantity,
        request.UnitPrice));

    var allowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Created", "Processing", "Completed", "Cancelled"
    };

    if (string.IsNullOrWhiteSpace(request.Status) || !allowedStatuses.Contains(request.Status.Trim()))
    {
        errors["status"] = ["Status must be one of: Created, Processing, Completed, Cancelled."];
    }

    return errors;
}

app.Run();

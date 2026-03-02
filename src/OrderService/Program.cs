var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var ordersApi = app.MapGroup("/api/v1/orders")
    .WithName("Orders")
    .WithOpenApi();

ordersApi.MapPost("/", CreateOrder)
    .WithName("CreateOrder")
    .WithOpenApi();

ordersApi.MapGet("/{id}", GetOrder)
    .WithName("GetOrder")
    .WithOpenApi();

ordersApi.MapGet("/", GetAllOrders)
    .WithName("GetAllOrders")
    .WithOpenApi();

ordersApi.MapPut("/{id}", UpdateOrder)
    .WithName("UpdateOrder")
    .WithOpenApi();

ordersApi.MapDelete("/{id}", DeleteOrder)
    .WithName("DeleteOrder")
    .WithOpenApi();

IResult CreateOrder()
{
    return Results.Ok(new { message = "Order created" });
}

IResult GetOrder(int id)
{
    return Results.Ok(new { id, message = "Order retrieved" });
}

IResult GetAllOrders()
{
    return Results.Ok(new { message = "All orders retrieved" });
}

IResult UpdateOrder(int id)
{
    return Results.Ok(new { id, message = "Order updated" });
}

IResult DeleteOrder(int id)
{
    return Results.Ok(new { id, message = "Order deleted" });
}

app.Run();

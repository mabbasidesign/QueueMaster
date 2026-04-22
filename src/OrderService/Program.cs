using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Services;
using OrderService.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token in the format: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var tenantId = builder.Configuration["Authentication:TenantId"]
    ?? throw new InvalidOperationException("Authentication:TenantId is required.");
var audience = builder.Configuration["Authentication:Audience"]
    ?? throw new InvalidOperationException("Authentication:Audience is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            RoleClaimType = "roles",
            NameClaimType = "name"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("QueueMaster.Admin"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("QueueMaster.User", "QueueMaster.Admin"));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register OrderService
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

// Register Service Bus
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection("ServiceBus"));
builder.Services.AddScoped<IServiceBusPublisher, ServiceBusPublisher>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

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
.RequireAuthorization("UserOrAdmin")
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
.RequireAuthorization("UserOrAdmin")
.WithOpenApi();

app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService orderService) =>
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
    
    // CreateOrderAsync now saves both Order and OutboxEvent in same transaction
    var created = await orderService.CreateOrderAsync(order);
    var totalAmount = created.Quantity * created.UnitPrice;
    
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
.RequireAuthorization("AdminOnly")
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
.RequireAuthorization("AdminOnly")
.WithOpenApi();

app.MapDelete("/api/orders/{id}", async (int id, IOrderService orderService) =>
{
    var result = await orderService.DeleteOrderAsync(id);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeleteOrder")
.RequireAuthorization("AdminOnly")
.WithOpenApi();

app.Run();

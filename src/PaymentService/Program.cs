using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentService.Data;
using PaymentService.Dtos;
using PaymentService.Services;
using PaymentService.Messaging;

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
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidIssuers = new[]
            {
                $"https://sts.windows.net/{tenantId}/",
                $"https://login.microsoftonline.com/{tenantId}/v2.0"
            },
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
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register PaymentService
builder.Services.AddScoped<IPaymentService, PaymentService.Services.PaymentService>();

// Register Service Bus
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection("ServiceBus"));
builder.Services.AddScoped<IServiceBusConsumer, ServiceBusConsumer>();
builder.Services.AddHostedService<ServiceBusConsumer>();

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

app.MapGet("/health", (PaymentDbContext dbContext) =>
{
    try
    {
        dbContext.Database.CanConnect();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("Health")
.WithOpenApi();

// Payment API Endpoints
app.MapGet("/api/payments", async (IPaymentService paymentService) =>
{
    var payments = await paymentService.GetAllPaymentsAsync();
    var response = payments.Select(p => new PaymentResponse(
        p.TransactionId,
        p.OrderId,
        p.Amount,
        p.Currency,
        p.Method,
        p.Status,
        p.CreatedAtUtc));
    return Results.Ok(response);
})
.WithName("GetAllPayments")
.RequireAuthorization("UserOrAdmin")
.WithOpenApi();

app.MapGet("/api/payments/{transactionId:guid}", async (Guid transactionId, IPaymentService paymentService) =>
{
    var payment = await paymentService.GetPaymentByTransactionIdAsync(transactionId);
    if (payment is null)
        return Results.NotFound();
    
    var response = new PaymentResponse(
        payment.TransactionId,
        payment.OrderId,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.Status,
        payment.CreatedAtUtc);
    return Results.Ok(response);
})
.WithName("GetPaymentByTransactionId")
.RequireAuthorization("UserOrAdmin")
.WithOpenApi();

app.MapGet("/api/payments/order/{orderId}", async (int orderId, IPaymentService paymentService) =>
{
    var payments = await paymentService.GetPaymentsByOrderIdAsync(orderId);
    var response = payments.Select(p => new PaymentResponse(
        p.TransactionId,
        p.OrderId,
        p.Amount,
        p.Currency,
        p.Method,
        p.Status,
        p.CreatedAtUtc));
    return Results.Ok(response);
})
.WithName("GetPaymentsByOrderId")
.RequireAuthorization("UserOrAdmin")
.WithOpenApi();

app.MapPost("/api/payments", async (ProcessPaymentRequest request, IPaymentService paymentService) =>
{
    var payment = new PaymentService.Models.Payment
    {
        TransactionId = Guid.NewGuid(),
        OrderId = request.OrderId,
        Amount = request.Amount,
        Currency = request.Currency,
        Method = request.Method,
        Status = "Pending",
        CreatedAtUtc = DateTime.UtcNow
    };
    
    var created = await paymentService.CreatePaymentAsync(payment);
    var response = new PaymentResponse(
        created.TransactionId,
        created.OrderId,
        created.Amount,
        created.Currency,
        created.Method,
        created.Status,
        created.CreatedAtUtc);
    return Results.Created($"/api/payments/{response.TransactionId}", response);
})
.WithName("CreatePayment")
.RequireAuthorization("AdminOnly")
.WithOpenApi();

app.MapPut("/api/payments/{transactionId:guid}", async (Guid transactionId, RefundPaymentRequest request, IPaymentService paymentService) =>
{
    var existing = await paymentService.GetPaymentByTransactionIdAsync(transactionId);
    if (existing is null)
        return Results.NotFound();
    
    existing.Status = "Refunded";
    
    var updated = await paymentService.UpdatePaymentAsync(existing);
    var response = new PaymentResponse(
        updated.TransactionId,
        updated.OrderId,
        updated.Amount,
        updated.Currency,
        updated.Method,
        updated.Status,
        updated.CreatedAtUtc);
    return Results.Ok(response);
})
.WithName("UpdatePayment")
.RequireAuthorization("AdminOnly")
.WithOpenApi();

app.MapDelete("/api/payments/{transactionId:guid}", async (Guid transactionId, IPaymentService paymentService) =>
{
    var result = await paymentService.DeletePaymentAsync(transactionId);
    return result ? Results.NoContent() : Results.NotFound();
})
.WithName("DeletePayment")
.RequireAuthorization("AdminOnly")
.WithOpenApi();

app.Run();

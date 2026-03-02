var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var paymentsApi = app.MapGroup("/api/v1/payments")
    .WithName("Payments")
    .WithOpenApi();

paymentsApi.MapPost("/process", ProcessPayment)
    .WithName("ProcessPayment")
    .WithOpenApi();

paymentsApi.MapGet("/status/{transactionId}", GetPaymentStatus)
    .WithName("GetPaymentStatus")
    .WithOpenApi();

paymentsApi.MapGet("/", GetAllPayments)
    .WithName("GetAllPayments")
    .WithOpenApi();

paymentsApi.MapPost("/refund/{transactionId}", RefundPayment)
    .WithName("RefundPayment")
    .WithOpenApi();

paymentsApi.MapGet("/health", HealthCheck)
    .WithName("HealthCheck")
    .WithOpenApi();

IResult ProcessPayment()
{
    var transactionId = Guid.NewGuid();
    return Results.Accepted($"/api/v1/payments/status/{transactionId}", new { transactionId, status = "Processing" });
}

IResult GetPaymentStatus(string transactionId)
{
    return Results.Ok(new { transactionId, status = "Completed", amount = 0.00m });
}

IResult GetAllPayments()
{
    return Results.Ok(new { payments = new List<object>() });
}

IResult RefundPayment(string transactionId)
{
    return Results.Ok(new { transactionId, status = "Refunded" });
}

IResult HealthCheck()
{
    return Results.Ok(new { status = "Payment service is healthy", timestamp = DateTime.UtcNow });
}

app.Run();

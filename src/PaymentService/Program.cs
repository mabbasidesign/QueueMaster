using PaymentService.Dtos;
using PaymentService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var payments = new List<Payment>();

var paymentsApi = app.MapGroup("/api/v1/payments")
    .WithName("Payments")
    .WithOpenApi();

paymentsApi.MapPost("/process", ProcessPayment)
    .WithName("ProcessPayment")
    .WithOpenApi();

paymentsApi.MapGet("/status/{transactionId:guid}", GetPaymentStatus)
    .WithName("GetPaymentStatus")
    .WithOpenApi();

paymentsApi.MapGet("/", GetAllPayments)
    .WithName("GetAllPayments")
    .WithOpenApi();

paymentsApi.MapPost("/refund/{transactionId:guid}", RefundPayment)
    .WithName("RefundPayment")
    .WithOpenApi();

paymentsApi.MapGet("/health", HealthCheck)
    .WithName("HealthCheck")
    .WithOpenApi();

IResult ProcessPayment(ProcessPaymentRequest request)
{
    var validationErrors = ValidateProcessPayment(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var payment = new Payment
    {
        TransactionId = Guid.NewGuid(),
        OrderId = request.OrderId,
        Amount = request.Amount,
        Currency = request.Currency.Trim().ToUpperInvariant(),
        Method = request.Method.Trim(),
        Status = "Completed"
    };

    payments.Add(payment);

    return Results.Accepted($"/api/v1/payments/status/{payment.TransactionId}", ToResponse(payment));
}

IResult GetPaymentStatus(Guid transactionId)
{
    var payment = payments.FirstOrDefault(item => item.TransactionId == transactionId);
    if (payment is null)
    {
        return Results.NotFound(new { message = $"Payment {transactionId} not found" });
    }

    return Results.Ok(ToResponse(payment));
}

IResult GetAllPayments()
{
    var response = payments.Select(ToResponse).ToList();
    return Results.Ok(response);
}

IResult RefundPayment(Guid transactionId, RefundPaymentRequest request)
{
    var validationErrors = ValidateRefundRequest(request);
    if (validationErrors.Count > 0)
    {
        return Results.ValidationProblem(validationErrors);
    }

    var payment = payments.FirstOrDefault(item => item.TransactionId == transactionId);
    if (payment is null)
    {
        return Results.NotFound(new { message = $"Payment {transactionId} not found" });
    }

    if (payment.Status.Equals("Refunded", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { message = "Payment is already refunded" });
    }

    if (request.Amount > payment.Amount)
    {
        return Results.BadRequest(new { message = "Refund amount cannot exceed payment amount" });
    }

    payment.Status = "Refunded";

    return Results.Ok(new
    {
        transactionId = payment.TransactionId,
        refundedAmount = request.Amount,
        reason = request.Reason.Trim(),
        status = payment.Status
    });
}

IResult HealthCheck()
{
    return Results.Ok(new { status = "Payment service is healthy", timestamp = DateTime.UtcNow });
}

static PaymentResponse ToResponse(Payment payment)
{
    return new PaymentResponse(
        payment.TransactionId,
        payment.OrderId,
        payment.Amount,
        payment.Currency,
        payment.Method,
        payment.Status,
        payment.CreatedAtUtc
    );
}

static Dictionary<string, string[]> ValidateProcessPayment(ProcessPaymentRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.OrderId <= 0)
    {
        errors["orderId"] = ["OrderId must be greater than zero."];
    }

    if (request.Amount <= 0)
    {
        errors["amount"] = ["Amount must be greater than zero."];
    }

    if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
    {
        errors["currency"] = ["Currency must be a 3-letter code (e.g., USD)."];
    }

    var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Card", "BankTransfer", "Wallet"
    };

    if (string.IsNullOrWhiteSpace(request.Method) || !allowedMethods.Contains(request.Method.Trim()))
    {
        errors["method"] = ["Method must be one of: Card, BankTransfer, Wallet."];
    }

    return errors;
}

static Dictionary<string, string[]> ValidateRefundRequest(RefundPaymentRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (request.Amount <= 0)
    {
        errors["amount"] = ["Refund amount must be greater than zero."];
    }

    if (string.IsNullOrWhiteSpace(request.Reason))
    {
        errors["reason"] = ["Refund reason is required."];
    }

    return errors;
}

app.Run();

using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments { get; set; }
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => 
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            
            entity.Property(e => e.OrderId)
                .IsRequired();

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(3);

            entity.Property(e => e.Method)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAtUtc);

            // Seed data
            entity.HasData(
                new Payment
                {
                    TransactionId = new Guid("11111111-1111-1111-1111-111111111111"),
                    OrderId = 1,
                    Amount = 999.99m,
                    Currency = "USD",
                    Method = "Card",
                    Status = "Completed",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
                },
                new Payment
                {
                    TransactionId = new Guid("22222222-2222-2222-2222-222222222222"),
                    OrderId = 2,
                    Amount = 599.98m,
                    Currency = "USD",
                    Method = "BankTransfer",
                    Status = "Completed",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new Payment
                {
                    TransactionId = new Guid("33333333-3333-3333-3333-333333333333"),
                    OrderId = 3,
                    Amount = 239.97m,
                    Currency = "USD",
                    Method = "Card",
                    Status = "Processing",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Payment
                {
                    TransactionId = new Guid("44444444-4444-4444-4444-444444444444"),
                    OrderId = 4,
                    Amount = 149.95m,
                    Currency = "USD",
                    Method = "Wallet",
                    Status = "Completed",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                }
            );
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.MessageId)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(e => e.ProcessedAtUtc)
                .IsRequired();

            entity.HasIndex(e => e.OrderId);
        });
    }
}

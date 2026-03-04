using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => 
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Quantity)
                .IsRequired();

            entity.Property(e => e.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(e => e.CustomerName);
            entity.HasIndex(e => e.Status);

            // Seed data
            entity.HasData(
                new Order
                {
                    Id = 1,
                    CustomerName = "John Smith",
                    ProductName = "Laptop",
                    Quantity = 1,
                    UnitPrice = 999.99m,
                    Status = "Completed",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
                },
                new Order
                {
                    Id = 2,
                    CustomerName = "Jane Doe",
                    ProductName = "Monitor",
                    Quantity = 2,
                    UnitPrice = 299.99m,
                    Status = "Processing",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
                },
                new Order
                {
                    Id = 3,
                    CustomerName = "Bob Johnson",
                    ProductName = "Keyboard",
                    Quantity = 3,
                    UnitPrice = 79.99m,
                    Status = "Created",
                    CreatedAtUtc = DateTime.UtcNow
                },
                new Order
                {
                    Id = 4,
                    CustomerName = "Alice Brown",
                    ProductName = "Mouse",
                    Quantity = 5,
                    UnitPrice = 29.99m,
                    Status = "Completed",
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-10)
                }
            );
        });

            modelBuilder.Entity<OutboxEvent>(entity =>
            {
                entity.HasKey(e => e.Id);
            
                entity.Property(e => e.EventType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Payload)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.IsPublished, e.CreatedAt });
            });
    }
}

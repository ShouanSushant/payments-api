using Microsoft.EntityFrameworkCore;
using PaymentsApi.Models;

namespace PaymentsApi.Data;

public class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Email).HasMaxLength(200).IsRequired();
            e.HasIndex(c => c.Email).IsUnique();
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.Description).HasMaxLength(200).IsRequired();
            // Store the status as readable text ("PartiallyPaid") instead of a number.
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.HasOne(i => i.Customer).WithMany(c => c.Invoices).HasForeignKey(i => i.CustomerId);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(p => p.IdempotencyKey).HasMaxLength(100);
            // The database itself refuses a second payment with the same key.
            e.HasIndex(p => p.IdempotencyKey).IsUnique();
            e.HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId);
        });

        modelBuilder.Entity<Refund>(e =>
        {
            e.Property(r => r.Reason).HasMaxLength(200).IsRequired();
            e.HasOne(r => r.Payment).WithMany(p => p.Refunds).HasForeignKey(r => r.PaymentId);
        });
    }
}

namespace PaymentsApi.Models;

// All money is stored as whole cents (long), never as double/float,
// so amounts like $0.10 + $0.20 never pick up rounding errors.

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<Invoice> Invoices { get; set; } = new();
}

public enum InvoiceStatus
{
    Open,
    PartiallyPaid,
    Paid,
    Void
}

public class Invoice
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public long TotalCents { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Payment> Payments { get; set; } = new();
}

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public long AmountCents { get; set; }

    // Sent by the client in the Idempotency-Key header. If the same request is
    // retried (double click, network timeout), the customer is not charged twice.
    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Refund> Refunds { get; set; } = new();
}

public class Refund
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public long AmountCents { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;
using PaymentsApi.Models;
using PaymentsApi.Services;

namespace PaymentsApi.Dtos;

// ---------- Requests (what clients send) ----------
// [ApiController] checks these attributes automatically and returns 400 if they fail,
// so bad input never reaches the service.

public class CreateCustomerRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
}

public class CreateInvoiceRequest
{
    [Range(1, int.MaxValue)]
    public int CustomerId { get; set; }

    // Max $1,000,000.00 per invoice.
    [Range(typeof(long), "1", "100000000")]
    public long TotalCents { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;
}

public class RecordPaymentRequest
{
    [Range(typeof(long), "1", "100000000")]
    public long AmountCents { get; set; }
}

public class RefundRequest
{
    [Range(typeof(long), "1", "100000000")]
    public long AmountCents { get; set; }

    [Required, StringLength(200, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;
}

// ---------- Responses (what the API returns) ----------
// Separate from the database entities, so internal fields never leak out
// and changing the database doesn't break clients.

public record CustomerDto(int Id, string Name, string Email);

public record PaymentDto(int Id, long AmountCents, long RefundedCents, DateTime CreatedAt);

public record InvoiceDto(
    int Id,
    int CustomerId,
    string CustomerName,
    string Description,
    long TotalCents,
    long PaidCents,
    long OutstandingCents,
    InvoiceStatus Status,
    DateTime CreatedAt,
    List<PaymentDto> Payments);

public record RefundDto(int Id, int PaymentId, long AmountCents, string Reason, DateTime CreatedAt);

public static class DtoMapper
{
    public static CustomerDto ToCustomerDto(Customer c) => new(c.Id, c.Name, c.Email);

    public static PaymentDto ToPaymentDto(Payment p) =>
        new(p.Id, p.AmountCents, p.Refunds.Sum(r => r.AmountCents), p.CreatedAt);

    public static InvoiceDto ToInvoiceDto(Invoice i) => new(
        i.Id,
        i.CustomerId,
        i.Customer?.Name ?? string.Empty,
        i.Description,
        i.TotalCents,
        InvoiceMath.NetPaidCents(i),
        InvoiceMath.OutstandingCents(i),
        i.Status,
        i.CreatedAt,
        i.Payments.OrderBy(p => p.Id).Select(p => ToPaymentDto(p)).ToList());

    public static RefundDto ToRefundDto(Refund r) =>
        new(r.Id, r.PaymentId, r.AmountCents, r.Reason, r.CreatedAt);
}

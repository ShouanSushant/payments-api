using Microsoft.EntityFrameworkCore;
using PaymentsApi.Data;
using PaymentsApi.Models;

namespace PaymentsApi.Services;

public interface IPaymentService
{
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer> CreateCustomerAsync(string name, string email);
    Task<List<Invoice>> GetInvoicesAsync();
    Task<Invoice> GetInvoiceAsync(int invoiceId);
    Task<Invoice> CreateInvoiceAsync(int customerId, long totalCents, string description);
    Task<Invoice> VoidInvoiceAsync(int invoiceId);
    Task<Payment> RecordPaymentAsync(int invoiceId, long amountCents, string? idempotencyKey);
    Task<Refund> RefundAsync(int paymentId, long amountCents, string reason);
}

// All business rules live here, not in the controllers. Controllers only
// translate HTTP to method calls, which keeps this class easy to test.
public class PaymentService : IPaymentService
{
    private readonly PaymentsDbContext _db;

    public PaymentService(PaymentsDbContext db)
    {
        _db = db;
    }

    public Task<List<Customer>> GetCustomersAsync() =>
        _db.Customers.OrderBy(c => c.Name).ToListAsync();

    public async Task<Customer> CreateCustomerAsync(string name, string email)
    {
        name = name.Trim();
        email = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PaymentRuleException("Customer name is required.");
        }

        if (await _db.Customers.AnyAsync(c => c.Email == email))
        {
            throw new PaymentRuleException("A customer with this email already exists.");
        }

        var customer = new Customer { Name = name, Email = email };
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public Task<List<Invoice>> GetInvoicesAsync() =>
        InvoicesWithDetails().OrderByDescending(i => i.Id).ToListAsync();

    public async Task<Invoice> GetInvoiceAsync(int invoiceId) =>
        await InvoicesWithDetails().FirstOrDefaultAsync(i => i.Id == invoiceId)
        ?? throw new NotFoundException($"Invoice {invoiceId} was not found.");

    public async Task<Invoice> CreateInvoiceAsync(int customerId, long totalCents, string description)
    {
        if (totalCents <= 0)
        {
            throw new PaymentRuleException("Invoice total must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new PaymentRuleException("Invoice description is required.");
        }

        var customer = await _db.Customers.FindAsync(customerId)
            ?? throw new NotFoundException($"Customer {customerId} was not found.");

        var invoice = new Invoice
        {
            Customer = customer,
            TotalCents = totalCents,
            Description = description.Trim()
        };

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Invoice> VoidInvoiceAsync(int invoiceId)
    {
        var invoice = await GetInvoiceAsync(invoiceId);

        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new PaymentRuleException("This invoice is already void.");
        }

        if (InvoiceMath.NetPaidCents(invoice) > 0)
        {
            throw new PaymentRuleException("Refund all payments before voiding this invoice.");
        }

        invoice.Status = InvoiceStatus.Void;
        await _db.SaveChangesAsync();
        return invoice;
    }

    public async Task<Payment> RecordPaymentAsync(int invoiceId, long amountCents, string? idempotencyKey)
    {
        if (amountCents <= 0)
        {
            throw new PaymentRuleException("Payment amount must be greater than zero.");
        }

        idempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();

        // Same key seen before: this is a retry, so hand back the original payment
        // instead of charging again.
        if (idempotencyKey is not null)
        {
            var existing = await _db.Payments
                .Include(p => p.Refunds)
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);

            if (existing is not null)
            {
                if (existing.InvoiceId != invoiceId || existing.AmountCents != amountCents)
                {
                    throw new PaymentRuleException("This Idempotency-Key was already used for a different payment.");
                }

                return existing;
            }
        }

        var invoice = await GetInvoiceAsync(invoiceId);

        if (invoice.Status == InvoiceStatus.Void)
        {
            throw new PaymentRuleException("Payments can't be made on a void invoice.");
        }

        var outstanding = InvoiceMath.OutstandingCents(invoice);
        if (amountCents > outstanding)
        {
            throw new PaymentRuleException(
                $"Payment of {amountCents} cents is more than the outstanding balance of {outstanding} cents.");
        }

        var payment = new Payment { AmountCents = amountCents, IdempotencyKey = idempotencyKey };
        invoice.Payments.Add(payment);
        invoice.Status = InvoiceMath.StatusFor(invoice);

        // The payment and the status change are saved in one transaction:
        // either both are written or neither is.
        await _db.SaveChangesAsync();
        return payment;
    }

    public async Task<Refund> RefundAsync(int paymentId, long amountCents, string reason)
    {
        if (amountCents <= 0)
        {
            throw new PaymentRuleException("Refund amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new PaymentRuleException("A refund reason is required.");
        }

        var payment = await _db.Payments
            .Include(p => p.Refunds)
            .Include(p => p.Invoice).ThenInclude(i => i.Payments).ThenInclude(p => p.Refunds)
            .FirstOrDefaultAsync(p => p.Id == paymentId)
            ?? throw new NotFoundException($"Payment {paymentId} was not found.");

        var refundable = InvoiceMath.RefundableCents(payment);
        if (amountCents > refundable)
        {
            throw new PaymentRuleException(
                $"Refund of {amountCents} cents is more than the {refundable} cents left on this payment.");
        }

        var refund = new Refund { AmountCents = amountCents, Reason = reason.Trim() };
        payment.Refunds.Add(refund);
        payment.Invoice.Status = InvoiceMath.StatusFor(payment.Invoice);

        await _db.SaveChangesAsync();
        return refund;
    }

    private IQueryable<Invoice> InvoicesWithDetails() =>
        _db.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Payments).ThenInclude(p => p.Refunds);
}

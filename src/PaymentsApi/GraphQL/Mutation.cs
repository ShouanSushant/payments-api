using System.ComponentModel.DataAnnotations;
using HotChocolate;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.GraphQL;

// GraphQL write side. Every mutation goes through the same PaymentService as
// the REST API, so the business rules (no overpaying, no double charges) are
// enforced in one place no matter which API a client uses.
public class Mutation
{
    // Same limit as the REST request DTOs: $1,000,000.00.
    private const long MaxCents = 100_000_000;

    public async Task<CustomerDto> CreateCustomerAsync(
        string name,
        string email,
        [Service] IPaymentService payments)
    {
        Require(name.Length <= 100, "Customer name must be 100 characters or fewer.");
        Require(email.Length <= 200 && new EmailAddressAttribute().IsValid(email), "Enter a valid email address.");

        var customer = await payments.CreateCustomerAsync(name, email);
        return DtoMapper.ToCustomerDto(customer);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(
        int customerId,
        long totalCents,
        string description,
        [Service] IPaymentService payments)
    {
        Require(totalCents <= MaxCents, "Invoice total can't be more than $1,000,000.00.");
        Require(description.Length <= 200, "Description must be 200 characters or fewer.");

        var created = await payments.CreateInvoiceAsync(customerId, totalCents, description);
        return DtoMapper.ToInvoiceDto(await payments.GetInvoiceAsync(created.Id));
    }

    public async Task<InvoiceDto> VoidInvoiceAsync(int invoiceId, [Service] IPaymentService payments)
    {
        var invoice = await payments.VoidInvoiceAsync(invoiceId);
        return DtoMapper.ToInvoiceDto(invoice);
    }

    // Returns the updated invoice so the client can show the new status and balance
    // without making a second request.
    [GraphQLDescription("Pays part or all of an invoice. Send the same idempotencyKey on a retry and the customer is only charged once.")]
    public async Task<InvoiceDto> RecordPaymentAsync(
        int invoiceId,
        long amountCents,
        string? idempotencyKey,
        [Service] IPaymentService payments)
    {
        Require(amountCents <= MaxCents, "Payment can't be more than $1,000,000.00.");
        Require(idempotencyKey is null || idempotencyKey.Length <= 100, "Idempotency key must be 100 characters or fewer.");

        await payments.RecordPaymentAsync(invoiceId, amountCents, idempotencyKey);
        return DtoMapper.ToInvoiceDto(await payments.GetInvoiceAsync(invoiceId));
    }

    public async Task<RefundDto> RefundPaymentAsync(
        int paymentId,
        long amountCents,
        string reason,
        [Service] IPaymentService payments)
    {
        Require(amountCents <= MaxCents, "Refund can't be more than $1,000,000.00.");
        Require(reason.Length <= 200, "Reason must be 200 characters or fewer.");

        var refund = await payments.RefundAsync(paymentId, amountCents, reason);
        return DtoMapper.ToRefundDto(refund);
    }

    // GraphQL doesn't run the [Range]/[Required] attributes the REST DTOs use,
    // so the same limits are checked here.
    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new PaymentRuleException(message);
        }
    }
}

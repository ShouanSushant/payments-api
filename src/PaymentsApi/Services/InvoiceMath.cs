using PaymentsApi.Models;

namespace PaymentsApi.Services;

// Pure calculations with no database access, so they are easy to unit test.
public static class InvoiceMath
{
    // How much is left to refund on one payment.
    public static long RefundableCents(Payment payment) =>
        payment.AmountCents - payment.Refunds.Sum(r => r.AmountCents);

    // Money actually kept on an invoice: every payment minus its refunds.
    public static long NetPaidCents(Invoice invoice) =>
        invoice.Payments.Sum(p => RefundableCents(p));

    public static long OutstandingCents(Invoice invoice) =>
        invoice.TotalCents - NetPaidCents(invoice);

    // The status always follows from the numbers, so it can never drift out of sync.
    public static InvoiceStatus StatusFor(Invoice invoice)
    {
        if (invoice.Status == InvoiceStatus.Void)
        {
            return InvoiceStatus.Void;
        }

        var paid = NetPaidCents(invoice);
        if (paid <= 0)
        {
            return InvoiceStatus.Open;
        }

        return paid >= invoice.TotalCents ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
    }
}

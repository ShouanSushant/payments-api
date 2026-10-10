using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentsApi.Data;
using PaymentsApi.Models;
using PaymentsApi.Services;

namespace PaymentsApi.Tests;

// xUnit creates a new instance of this class for every test, so each test
// gets a fresh in-memory SQLite database: a real SQL engine (same as the app)
// that leaves nothing behind.
public class PaymentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PaymentsDbContext _db;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PaymentsDbContext(options);
        _db.Database.EnsureCreated();
        _service = new PaymentService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Invoice> NewInvoiceAsync(long totalCents = 10_000)
    {
        var customer = await _service.CreateCustomerAsync("Test Co", $"test-{Guid.NewGuid():N}@example.com");
        return await _service.CreateInvoiceAsync(customer.Id, totalCents, "Test invoice");
    }

    // ---------- Payments ----------

    [Fact]
    public async Task RecordPayment_FullAmount_MarksInvoicePaid()
    {
        // Arrange
        var invoice = await NewInvoiceAsync(10_000);

        // Act
        await _service.RecordPaymentAsync(invoice.Id, 10_000, null);

        // Assert
        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.Paid, updated.Status);
        Assert.Equal(0L, InvoiceMath.OutstandingCents(updated));
    }

    [Fact]
    public async Task RecordPayment_PartialAmount_MarksInvoicePartiallyPaid()
    {
        var invoice = await NewInvoiceAsync(10_000);

        await _service.RecordPaymentAsync(invoice.Id, 2_500, null);

        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.PartiallyPaid, updated.Status);
        Assert.Equal(7_500L, InvoiceMath.OutstandingCents(updated));
    }

    [Fact]
    public async Task RecordPayment_MoreThanOutstanding_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        await _service.RecordPaymentAsync(invoice.Id, 6_000, null);

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 4_001, null));
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-500L)]
    public async Task RecordPayment_ZeroOrNegative_IsRejected(long amountCents)
    {
        var invoice = await NewInvoiceAsync();

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, amountCents, null));
    }

    [Fact]
    public async Task RecordPayment_SameIdempotencyKey_ChargesOnlyOnce()
    {
        var invoice = await NewInvoiceAsync(10_000);

        // Simulates the client retrying the same request after a timeout.
        var first = await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");
        var retry = await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");

        Assert.Equal(first.Id, retry.Id);
        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.Single(updated.Payments);
        Assert.Equal(3_000L, InvoiceMath.NetPaidCents(updated));
    }

    [Fact]
    public async Task RecordPayment_ReusedKeyWithDifferentAmount_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 5_000, "order-123"));
    }

    [Fact]
    public async Task RecordPayment_OnVoidInvoice_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        await _service.VoidInvoiceAsync(invoice.Id);

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 1_000, null));
    }

    [Fact]
    public async Task RecordPayment_UnknownInvoice_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.RecordPaymentAsync(9_999, 1_000, null));
    }

    // ---------- Refunds ----------

    [Fact]
    public async Task Refund_PartOfFullPayment_ReopensToPartiallyPaid()
    {
        var invoice = await NewInvoiceAsync(10_000);
        var payment = await _service.RecordPaymentAsync(invoice.Id, 10_000, null);

        await _service.RefundAsync(payment.Id, 4_000, "Damaged item");

        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.Equal(InvoiceStatus.PartiallyPaid, updated.Status);
        Assert.Equal(4_000L, InvoiceMath.OutstandingCents(updated));
    }

    [Fact]
    public async Task Refund_MoreThanWasPaid_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        var payment = await _service.RecordPaymentAsync(invoice.Id, 5_000, null);
        await _service.RefundAsync(payment.Id, 3_000, "First refund");

        // Only 2,000 cents left on this payment.
        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RefundAsync(payment.Id, 2_001, "Second refund"));
    }

    [Fact]
    public async Task Refund_WithoutReason_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        var payment = await _service.RecordPaymentAsync(invoice.Id, 1_000, null);

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.RefundAsync(payment.Id, 500, "  "));
    }

    // ---------- Invoices and customers ----------

    [Fact]
    public async Task VoidInvoice_WithMoneyStillPaid_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        await _service.RecordPaymentAsync(invoice.Id, 1_000, null);

        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.VoidInvoiceAsync(invoice.Id));
    }

    [Fact]
    public async Task VoidInvoice_AfterFullRefund_Succeeds()
    {
        var invoice = await NewInvoiceAsync();
        var payment = await _service.RecordPaymentAsync(invoice.Id, 1_000, null);
        await _service.RefundAsync(payment.Id, 1_000, "Cancelled order");

        var voided = await _service.VoidInvoiceAsync(invoice.Id);

        Assert.Equal(InvoiceStatus.Void, voided.Status);
    }

    [Fact]
    public async Task CreateCustomer_DuplicateEmail_IsRejected()
    {
        await _service.CreateCustomerAsync("First", "same@example.com");

        // Different casing and spaces should still count as the same email.
        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _service.CreateCustomerAsync("Second", "  SAME@example.com "));
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentsApi.Data;
using PaymentsApi.Models;
using PaymentsApi.Services;

namespace PaymentsApi.Tests;

// Each test gets a fresh in-memory SQLite database, so tests run against
// a real SQL engine (same as the app) but leave nothing behind.
[TestClass]
public class PaymentServiceTests
{
    private SqliteConnection _connection = null!;
    private PaymentsDbContext _db = null!;
    private PaymentService _service = null!;

    [TestInitialize]
    public void Setup()
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

    [TestCleanup]
    public void Cleanup()
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

    [TestMethod]
    public async Task RecordPayment_FullAmount_MarksInvoicePaid()
    {
        // Arrange
        var invoice = await NewInvoiceAsync(10_000);

        // Act
        await _service.RecordPaymentAsync(invoice.Id, 10_000, null);

        // Assert
        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.AreEqual(InvoiceStatus.Paid, updated.Status);
        Assert.AreEqual(0L, InvoiceMath.OutstandingCents(updated));
    }

    [TestMethod]
    public async Task RecordPayment_PartialAmount_MarksInvoicePartiallyPaid()
    {
        var invoice = await NewInvoiceAsync(10_000);

        await _service.RecordPaymentAsync(invoice.Id, 2_500, null);

        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.AreEqual(InvoiceStatus.PartiallyPaid, updated.Status);
        Assert.AreEqual(7_500L, InvoiceMath.OutstandingCents(updated));
    }

    [TestMethod]
    public async Task RecordPayment_MoreThanOutstanding_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        await _service.RecordPaymentAsync(invoice.Id, 6_000, null);

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 4_001, null));
    }

    [DataTestMethod]
    [DataRow(0L)]
    [DataRow(-500L)]
    public async Task RecordPayment_ZeroOrNegative_IsRejected(long amountCents)
    {
        var invoice = await NewInvoiceAsync();

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, amountCents, null));
    }

    [TestMethod]
    public async Task RecordPayment_SameIdempotencyKey_ChargesOnlyOnce()
    {
        var invoice = await NewInvoiceAsync(10_000);

        // Simulates the client retrying the same request after a timeout.
        var first = await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");
        var retry = await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");

        Assert.AreEqual(first.Id, retry.Id);
        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.AreEqual(1, updated.Payments.Count);
        Assert.AreEqual(3_000L, InvoiceMath.NetPaidCents(updated));
    }

    [TestMethod]
    public async Task RecordPayment_ReusedKeyWithDifferentAmount_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        await _service.RecordPaymentAsync(invoice.Id, 3_000, "order-123");

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 5_000, "order-123"));
    }

    [TestMethod]
    public async Task RecordPayment_OnVoidInvoice_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        await _service.VoidInvoiceAsync(invoice.Id);

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RecordPaymentAsync(invoice.Id, 1_000, null));
    }

    [TestMethod]
    public async Task RecordPayment_UnknownInvoice_ThrowsNotFound()
    {
        await Assert.ThrowsExceptionAsync<NotFoundException>(
            () => _service.RecordPaymentAsync(9_999, 1_000, null));
    }

    // ---------- Refunds ----------

    [TestMethod]
    public async Task Refund_PartOfFullPayment_ReopensToPartiallyPaid()
    {
        var invoice = await NewInvoiceAsync(10_000);
        var payment = await _service.RecordPaymentAsync(invoice.Id, 10_000, null);

        await _service.RefundAsync(payment.Id, 4_000, "Damaged item");

        var updated = await _service.GetInvoiceAsync(invoice.Id);
        Assert.AreEqual(InvoiceStatus.PartiallyPaid, updated.Status);
        Assert.AreEqual(4_000L, InvoiceMath.OutstandingCents(updated));
    }

    [TestMethod]
    public async Task Refund_MoreThanWasPaid_IsRejected()
    {
        var invoice = await NewInvoiceAsync(10_000);
        var payment = await _service.RecordPaymentAsync(invoice.Id, 5_000, null);
        await _service.RefundAsync(payment.Id, 3_000, "First refund");

        // Only 2,000 cents left on this payment.
        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RefundAsync(payment.Id, 2_001, "Second refund"));
    }

    [TestMethod]
    public async Task Refund_WithoutReason_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        var payment = await _service.RecordPaymentAsync(invoice.Id, 1_000, null);

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.RefundAsync(payment.Id, 500, "  "));
    }

    // ---------- Invoices and customers ----------

    [TestMethod]
    public async Task VoidInvoice_WithMoneyStillPaid_IsRejected()
    {
        var invoice = await NewInvoiceAsync();
        await _service.RecordPaymentAsync(invoice.Id, 1_000, null);

        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.VoidInvoiceAsync(invoice.Id));
    }

    [TestMethod]
    public async Task VoidInvoice_AfterFullRefund_Succeeds()
    {
        var invoice = await NewInvoiceAsync();
        var payment = await _service.RecordPaymentAsync(invoice.Id, 1_000, null);
        await _service.RefundAsync(payment.Id, 1_000, "Cancelled order");

        var voided = await _service.VoidInvoiceAsync(invoice.Id);

        Assert.AreEqual(InvoiceStatus.Void, voided.Status);
    }

    [TestMethod]
    public async Task CreateCustomer_DuplicateEmail_IsRejected()
    {
        await _service.CreateCustomerAsync("First", "same@example.com");

        // Different casing and spaces should still count as the same email.
        await Assert.ThrowsExceptionAsync<PaymentRuleException>(
            () => _service.CreateCustomerAsync("Second", "  SAME@example.com "));
    }
}

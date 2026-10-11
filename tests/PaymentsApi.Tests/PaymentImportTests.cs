using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PaymentsApi.Data;
using PaymentsApi.Models;
using PaymentsApi.Services;

namespace PaymentsApi.Tests;

public class PaymentImportTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PaymentsDbContext _db;
    private readonly PaymentService _service;
    private readonly PaymentImportService _import;

    public PaymentImportTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PaymentsDbContext(options);
        _db.Database.EnsureCreated();
        _service = new PaymentService(_db);
        _import = new PaymentImportService(_db, _service);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Stream Xml(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    private async Task<int> NewInvoiceAsync(long totalCents = 10_000)
    {
        var customer = await _service.CreateCustomerAsync("Test Co", $"test-{Guid.NewGuid():N}@example.com");
        var invoice = await _service.CreateInvoiceAsync(customer.Id, totalCents, "Test invoice");
        return invoice.Id;
    }

    [Fact]
    public async Task Import_ValidBatch_RecordsEachPayment()
    {
        var invoiceId = await NewInvoiceAsync(10_000);

        var result = await _import.ImportXmlAsync(Xml($"""
            <PaymentBatch>
              <Payment reference="BANK-1" invoiceId="{invoiceId}" amountCents="4000" />
              <Payment reference="BANK-2" invoiceId="{invoiceId}" amountCents="6000" />
            </PaymentBatch>
            """));

        Assert.Equal(2, result.Imported);
        Assert.Equal(0, result.Failed);
        var invoice = await _service.GetInvoiceAsync(invoiceId);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public async Task Import_SameFileTwice_DoesNotChargeTwice()
    {
        var invoiceId = await NewInvoiceAsync(10_000);
        var file = $"""<PaymentBatch><Payment reference="BANK-1" invoiceId="{invoiceId}" amountCents="2500" /></PaymentBatch>""";

        await _import.ImportXmlAsync(Xml(file));
        var second = await _import.ImportXmlAsync(Xml(file));

        Assert.Equal(0, second.Imported);
        Assert.Equal(1, second.AlreadyImported);
        var invoice = await _service.GetInvoiceAsync(invoiceId);
        Assert.Equal(2_500L, InvoiceMath.NetPaidCents(invoice));
    }

    [Fact]
    public async Task Import_BadRows_FailOnTheirOwn()
    {
        var invoiceId = await NewInvoiceAsync(10_000);

        var result = await _import.ImportXmlAsync(Xml($"""
            <PaymentBatch>
              <Payment reference="OK-1" invoiceId="{invoiceId}" amountCents="3000" />
              <Payment reference="TOO-MUCH" invoiceId="{invoiceId}" amountCents="99999" />
              <Payment reference="NO-INVOICE" invoiceId="9999" amountCents="100" />
              <Payment reference="BAD-AMOUNT" invoiceId="{invoiceId}" amountCents="25.50" />
            </PaymentBatch>
            """));

        Assert.Equal(1, result.Imported);
        Assert.Equal(3, result.Failed);
        Assert.Equal(ImportOutcome.Imported, result.Rows[0].Outcome);
        Assert.All(result.Rows.Skip(1), row => Assert.Equal(ImportOutcome.Failed, row.Outcome));
        Assert.Contains("outstanding balance", result.Rows[1].Message);
    }

    [Fact]
    public async Task Import_MalformedXml_IsRejected()
    {
        await Assert.ThrowsAsync<PaymentRuleException>(
            () => _import.ImportXmlAsync(Xml("<PaymentBatch><Payment reference=")));
    }

    [Fact]
    public async Task Import_FileWithDtd_IsRejected()
    {
        // A classic XXE attempt: the DTD tries to pull a local file into the document.
        var xxe = """
            <?xml version="1.0"?>
            <!DOCTYPE PaymentBatch [ <!ENTITY secret SYSTEM "file:///etc/passwd"> ]>
            <PaymentBatch><Payment reference="&secret;" invoiceId="1" amountCents="100" /></PaymentBatch>
            """;

        var error = await Assert.ThrowsAsync<PaymentRuleException>(() => _import.ImportXmlAsync(Xml(xxe)));
        Assert.Contains("isn't valid XML", error.Message);
    }
}

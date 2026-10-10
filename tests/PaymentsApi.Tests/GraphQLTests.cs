using System.Text.Json;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentsApi.Data;
using PaymentsApi.GraphQL;
using PaymentsApi.Services;

namespace PaymentsApi.Tests;

// Runs real GraphQL operations through the same schema the app uses
// (AddPaymentsGraphQL), backed by a fresh in-memory SQLite database.
public class GraphQLTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public GraphQLTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<PaymentsDbContext>(options => options.UseSqlite(_connection));
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddPaymentsGraphQL();
        _services = services.BuildServiceProvider();

        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.EnsureCreated();
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }

    private async Task<JsonElement> RunAsync(string operation)
    {
        var executor = await _services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        var result = await executor.ExecuteAsync(operation);
        return JsonDocument.Parse(result.ToJson()).RootElement;
    }

    // Creates a customer and a $100.00 invoice, returns the invoice id.
    private async Task<int> NewInvoiceAsync()
    {
        var customer = await RunAsync(
            $$"""mutation { createCustomer(name: "Test Co", email: "test-{{Guid.NewGuid():N}}@example.com") { id } }""");
        var customerId = customer.GetProperty("data").GetProperty("createCustomer").GetProperty("id").GetInt32();

        var invoice = await RunAsync(
            $$"""mutation { createInvoice(customerId: {{customerId}}, totalCents: 10000, description: "Test invoice") { id } }""");
        return invoice.GetProperty("data").GetProperty("createInvoice").GetProperty("id").GetInt32();
    }

    private static string ErrorCode(JsonElement result) =>
        result.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString()!;

    [Fact]
    public async Task Query_CustomersAndInvoicesTogether_ReturnsBoth()
    {
        // Both root fields share one DbContext, so this fails if they run in parallel.
        await NewInvoiceAsync();

        var result = await RunAsync("{ customers { id name } invoices { id status totalCents outstandingCents } }");

        Assert.False(result.TryGetProperty("errors", out _), result.ToString());
        var data = result.GetProperty("data");
        Assert.Equal(1, data.GetProperty("customers").GetArrayLength());
        var invoice = data.GetProperty("invoices")[0];
        Assert.Equal("OPEN", invoice.GetProperty("status").GetString());
        Assert.Equal(10_000L, invoice.GetProperty("outstandingCents").GetInt64());
    }

    [Fact]
    public async Task RecordPayment_FullAmount_ReturnsPaidInvoice()
    {
        var invoiceId = await NewInvoiceAsync();

        var result = await RunAsync(
            $$"""mutation { recordPayment(invoiceId: {{invoiceId}}, amountCents: 10000) { status outstandingCents } }""");

        var invoice = result.GetProperty("data").GetProperty("recordPayment");
        Assert.Equal("PAID", invoice.GetProperty("status").GetString());
        Assert.Equal(0L, invoice.GetProperty("outstandingCents").GetInt64());
    }

    [Fact]
    public async Task RecordPayment_SameIdempotencyKey_ChargesOnlyOnce()
    {
        var invoiceId = await NewInvoiceAsync();
        var pay = $$"""mutation { recordPayment(invoiceId: {{invoiceId}}, amountCents: 2500, idempotencyKey: "retry-1") { paidCents payments { id } } }""";

        await RunAsync(pay);
        var retry = await RunAsync(pay);

        var invoice = retry.GetProperty("data").GetProperty("recordPayment");
        Assert.Equal(1, invoice.GetProperty("payments").GetArrayLength());
        Assert.Equal(2_500L, invoice.GetProperty("paidCents").GetInt64());
    }

    [Fact]
    public async Task RecordPayment_MoreThanOutstanding_ReturnsPaymentRuleError()
    {
        var invoiceId = await NewInvoiceAsync();

        var result = await RunAsync(
            $$"""mutation { recordPayment(invoiceId: {{invoiceId}}, amountCents: 10001) { id } }""");

        Assert.Equal("PAYMENT_RULE", ErrorCode(result));
    }

    [Fact]
    public async Task Invoice_UnknownId_ReturnsNotFoundError()
    {
        var result = await RunAsync("{ invoice(id: 9999) { id } }");

        Assert.Equal("NOT_FOUND", ErrorCode(result));
    }

    [Fact]
    public async Task CreateCustomer_InvalidEmail_IsRejected()
    {
        var result = await RunAsync("""mutation { createCustomer(name: "Bad Email Co", email: "not-an-email") { id } }""");

        Assert.Equal("PAYMENT_RULE", ErrorCode(result));
    }
}

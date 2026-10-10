using HotChocolate;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.GraphQL;

// GraphQL read side. Hot Chocolate turns each public method into a field and
// drops the "Get" prefix and "Async" suffix, so GetInvoicesAsync becomes `invoices`.
// Like the REST controllers, these only call the service and map to DTOs.
public class Query
{
    public async Task<List<CustomerDto>> GetCustomersAsync([Service] IPaymentService payments)
    {
        var customers = await payments.GetCustomersAsync();
        return customers.Select(DtoMapper.ToCustomerDto).ToList();
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync([Service] IPaymentService payments)
    {
        var invoices = await payments.GetInvoicesAsync();
        return invoices.Select(DtoMapper.ToInvoiceDto).ToList();
    }

    public async Task<InvoiceDto> GetInvoiceAsync(int id, [Service] IPaymentService payments)
    {
        var invoice = await payments.GetInvoiceAsync(id);
        return DtoMapper.ToInvoiceDto(invoice);
    }
}

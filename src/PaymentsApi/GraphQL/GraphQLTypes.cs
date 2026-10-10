using HotChocolate;
using HotChocolate.Types;
using PaymentsApi.Dtos;

namespace PaymentsApi.GraphQL;

// The GraphQL schema exposes the same DTOs as the REST API, so database
// entities never leak out. These classes only give them clean type names
// (Invoice instead of InvoiceDto).

public class CustomerType : ObjectType<CustomerDto>
{
    protected override void Configure(IObjectTypeDescriptor<CustomerDto> descriptor) =>
        descriptor.Name("Customer");
}

public class InvoiceType : ObjectType<InvoiceDto>
{
    protected override void Configure(IObjectTypeDescriptor<InvoiceDto> descriptor)
    {
        descriptor.Name("Invoice");
        descriptor.Field(i => i.PaidCents).Description("Money kept on the invoice: payments minus refunds.");
        descriptor.Field(i => i.OutstandingCents).Description("What the customer still owes.");
    }
}

public class PaymentType : ObjectType<PaymentDto>
{
    protected override void Configure(IObjectTypeDescriptor<PaymentDto> descriptor) =>
        descriptor.Name("Payment");
}

public class RefundType : ObjectType<RefundDto>
{
    protected override void Configure(IObjectTypeDescriptor<RefundDto> descriptor) =>
        descriptor.Name("Refund");
}

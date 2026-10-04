using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IPaymentService _payments;

    public InvoicesController(IPaymentService payments)
    {
        _payments = payments;
    }

    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll()
    {
        var invoices = await _payments.GetInvoicesAsync();
        return invoices.Select(i => DtoMapper.ToInvoiceDto(i)).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvoiceDto>> Get(int id)
    {
        var invoice = await _payments.GetInvoiceAsync(id);
        return DtoMapper.ToInvoiceDto(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(CreateInvoiceRequest request)
    {
        var created = await _payments.CreateInvoiceAsync(request.CustomerId, request.TotalCents, request.Description);
        var invoice = await _payments.GetInvoiceAsync(created.Id);
        return Created($"/api/invoices/{invoice.Id}", DtoMapper.ToInvoiceDto(invoice));
    }

    [HttpPost("{id:int}/void")]
    public async Task<ActionResult<InvoiceDto>> Void(int id)
    {
        var invoice = await _payments.VoidInvoiceAsync(id);
        return DtoMapper.ToInvoiceDto(invoice);
    }

    // POST /api/invoices/5/payments
    // Header: Idempotency-Key: <any unique string, e.g. a GUID>
    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<InvoiceDto>> Pay(
        int id,
        RecordPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        await _payments.RecordPaymentAsync(id, request.AmountCents, idempotencyKey);
        var invoice = await _payments.GetInvoiceAsync(id);
        return DtoMapper.ToInvoiceDto(invoice);
    }
}

using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly IPaymentImportService _import;

    public PaymentsController(IPaymentService payments, IPaymentImportService import)
    {
        _payments = payments;
        _import = import;
    }

    // POST /api/payments/3/refunds
    [HttpPost("{id:int}/refunds")]
    public async Task<ActionResult<RefundDto>> Refund(int id, RefundRequest request)
    {
        var refund = await _payments.RefundAsync(id, request.AmountCents, request.Reason);
        return Created($"/api/payments/{id}/refunds/{refund.Id}", DtoMapper.ToRefundDto(refund));
    }

    // POST /api/payments/import
    // Content-Type: application/xml, body is a <PaymentBatch> file (see samples/payment-batch.xml).
    // Returns a JSON summary with the outcome of every row.
    [HttpPost("import")]
    [Consumes("application/xml", "text/xml")]
    [RequestSizeLimit(1_000_000)]
    public async Task<ActionResult<ImportResult>> Import(CancellationToken cancellationToken)
    {
        // Read the (size-limited) body into memory first. Kestrel doesn't allow
        // synchronous reads from the request stream, and this keeps the parser simple.
        using var file = new MemoryStream();
        await Request.Body.CopyToAsync(file, cancellationToken);
        file.Position = 0;

        return await _import.ImportXmlAsync(file, cancellationToken);
    }
}

using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    // POST /api/payments/3/refunds
    [HttpPost("{id:int}/refunds")]
    public async Task<ActionResult<RefundDto>> Refund(int id, RefundRequest request)
    {
        var refund = await _payments.RefundAsync(id, request.AmountCents, request.Reason);
        return Created($"/api/payments/{id}/refunds/{refund.Id}", DtoMapper.ToRefundDto(refund));
    }
}

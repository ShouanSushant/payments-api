using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Dtos;
using PaymentsApi.Services;

namespace PaymentsApi.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IPaymentService _payments;

    public CustomersController(IPaymentService payments)
    {
        _payments = payments;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll()
    {
        var customers = await _payments.GetCustomersAsync();
        return customers.Select(c => DtoMapper.ToCustomerDto(c)).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request)
    {
        var customer = await _payments.CreateCustomerAsync(request.Name, request.Email);
        return Created($"/api/customers/{customer.Id}", DtoMapper.ToCustomerDto(customer));
    }
}

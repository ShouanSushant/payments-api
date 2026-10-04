namespace PaymentsApi.Services;

// Thrown when a request breaks a business rule (overpaying, refunding too much, etc.).
// The error middleware turns this into a 400 Bad Request.
public class PaymentRuleException : Exception
{
    public PaymentRuleException(string message) : base(message)
    {
    }
}

// Thrown when a customer, invoice or payment doesn't exist. Becomes a 404.
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

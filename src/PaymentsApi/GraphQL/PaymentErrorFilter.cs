using HotChocolate;
using PaymentsApi.Services;

namespace PaymentsApi.GraphQL;

// The GraphQL version of ErrorHandlingMiddleware. Business rule errors come back
// with their real message and a code the client can check; anything unexpected
// keeps Hot Chocolate's generic "Unexpected Execution Error" so internals don't leak.
public class PaymentErrorFilter : IErrorFilter
{
    public IError OnError(IError error) => error.Exception switch
    {
        NotFoundException ex => error.WithMessage(ex.Message).WithCode("NOT_FOUND").RemoveException(),
        PaymentRuleException ex => error.WithMessage(ex.Message).WithCode("PAYMENT_RULE").RemoveException(),
        _ => error
    };
}

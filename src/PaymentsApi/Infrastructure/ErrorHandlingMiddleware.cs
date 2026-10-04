using Microsoft.AspNetCore.Mvc;
using PaymentsApi.Services;

namespace PaymentsApi.Infrastructure;

// Turns known exceptions into clean JSON errors (RFC 7807 "problem details")
// instead of a stack trace. Unknown errors become a generic 500 so internals don't leak.
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, StatusCodes.Status404NotFound, "Not found", ex.Message);
        }
        catch (PaymentRuleException ex)
        {
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Request not allowed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error on {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "Server error",
                "Something went wrong. Please try again.");
        }
    }

    private static Task WriteProblem(HttpContext context, int status, string title, string detail)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        });
    }
}

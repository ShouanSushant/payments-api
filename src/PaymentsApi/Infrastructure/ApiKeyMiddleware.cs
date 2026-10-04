using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace PaymentsApi.Infrastructure;

// Every /api request needs an X-Api-Key header.
// The real key is never stored: config only holds its SHA-256 hash.
public class ApiKeyMiddleware
{
    public const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly byte[] _expectedHash;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        var hex = config["Security:ApiKeySha256"]?.Trim();

        // Fail fast with a clear message if the setting holds the key itself instead of its hash.
        if (hex is null || hex.Length != 64 || !hex.All(Uri.IsHexDigit))
        {
            throw new InvalidOperationException(
                "Security:ApiKeySha256 must be the 64-character SHA-256 hash of your API key, not the key itself. " +
                "See 'Changing the API key' in README.md.");
        }

        _expectedHash = Convert.FromHexString(hex);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // The dashboard page and Swagger are public; only the API itself is protected.
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var provided = context.Request.Headers[HeaderName].ToString();
        if (!ApiKeyValidator.Matches(provided, _expectedHash))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Missing or invalid API key."
            });
            return;
        }

        await _next(context);
    }
}

public static class ApiKeyValidator
{
    public static byte[] Hash(string key) => SHA256.HashData(Encoding.UTF8.GetBytes(key));

    // FixedTimeEquals takes the same time whether the first or last byte differs,
    // so an attacker can't guess the key one character at a time by timing responses.
    public static bool Matches(string? provided, byte[] expectedHash) =>
        !string.IsNullOrEmpty(provided)
        && CryptographicOperations.FixedTimeEquals(Hash(provided), expectedHash);
}

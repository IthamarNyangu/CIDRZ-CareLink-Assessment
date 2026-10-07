using Microsoft.Extensions.Primitives;

namespace CareLink.Api.Middleware;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context.Request.Headers[HeaderName]);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object>
               {
                   ["CorrelationId"] = correlationId
               }))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(StringValues suppliedValue)
    {
        var value = suppliedValue.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(value) &&
            value.Length <= 100 &&
            !value.Any(char.IsControl))
        {
            return value;
        }

        return Guid.NewGuid().ToString("N");
    }
}

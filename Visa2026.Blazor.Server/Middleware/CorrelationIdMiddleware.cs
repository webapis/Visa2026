using Sentry;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate next;

    public CorrelationIdMiddleware(RequestDelegate next) => this.next = next;

    public async Task InvokeAsync(
        HttpContext context,
        ApplicationRuntimeLogContextAccessor runtimeLogContextAccessor)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        context.Items[ItemKey] = correlationId;

        if (SentrySdk.IsEnabled)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.SetTag("correlation_id", correlationId);
                var path = context.Request.Path.Value;
                if (!string.IsNullOrWhiteSpace(path))
                    scope.SetTag("request_path", path);
            });
        }

        using (runtimeLogContextAccessor.BeginScope())
        {
            runtimeLogContextAccessor.SetCorrelationId(correlationId);
            runtimeLogContextAccessor.SetRequestPath(context.Request.Path.Value);
            await next(context).ConfigureAwait(false);
        }
    }
}

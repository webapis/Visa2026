using System.Threading;

namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>Request-scoped correlation id, path, and user for runtime log rows.</summary>
public sealed class ApplicationRuntimeLogContextAccessor
{
    private static readonly AsyncLocal<RuntimeLogContext?> Current = new();

    public RuntimeLogContext? Context => Current.Value;

    public void SetCorrelationId(string? correlationId)
    {
        var ctx = Current.Value ?? new RuntimeLogContext();
        ctx.CorrelationId = correlationId;
        Current.Value = ctx;
    }

    public void SetRequestPath(string? requestPath)
    {
        var ctx = Current.Value ?? new RuntimeLogContext();
        ctx.RequestPath = requestPath;
        Current.Value = ctx;
    }

    public void SetUserName(string? userName)
    {
        var ctx = Current.Value ?? new RuntimeLogContext();
        ctx.UserName = userName;
        Current.Value = ctx;
    }

    public IDisposable BeginScope()
    {
        var previous = Current.Value;
        Current.Value = new RuntimeLogContext();
        return new ScopeDisposable(previous);
    }

    public sealed class RuntimeLogContext
    {
        public string? CorrelationId { get; set; }
        public string? RequestPath { get; set; }
        public string? UserName { get; set; }
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly RuntimeLogContext? previous;

        public ScopeDisposable(RuntimeLogContext? previous) => this.previous = previous;

        public void Dispose() => Current.Value = previous;
    }
}

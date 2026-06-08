namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class NullApplicationRuntimeLogSentryBridge : IApplicationRuntimeLogSentryBridge
{
    public string? TryCapture(ApplicationRuntimeLogEntry entry) => null;
}

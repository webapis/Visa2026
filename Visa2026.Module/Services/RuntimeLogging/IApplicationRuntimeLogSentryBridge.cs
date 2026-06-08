namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>
/// Optional Sentry capture for persisted <see cref="ApplicationRuntimeLog"/> rows (Phase 4).
/// Host supplies implementation; Module uses <see cref="NullApplicationRuntimeLogSentryBridge"/> by default.
/// </summary>
public interface IApplicationRuntimeLogSentryBridge
{
    /// <summary>Returns Sentry event id (32-char hex) when captured; otherwise null.</summary>
    string? TryCapture(ApplicationRuntimeLogEntry entry);
}

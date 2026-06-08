using System.Collections.Concurrent;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>
/// Captures runtime log entries during XAF <c>CheckCompatibility</c> before the host service provider
/// is fully wired. Pending rows flush when <see cref="Attach"/> runs (Blazor <c>Startup.Configure</c>).
/// </summary>
public static class ApplicationRuntimeLogStartupCapture
{
    private static readonly ConcurrentQueue<ApplicationRuntimeLogEntry> Pending = new();
    private static ApplicationRuntimeLogQueue? queue;

    internal static void ResetForTesting()
    {
        queue = null;
        while (Pending.TryDequeue(out _)) { }
    }

    public static void Attach(ApplicationRuntimeLogQueue runtimeLogQueue)
    {
        queue = runtimeLogQueue ?? throw new ArgumentNullException(nameof(runtimeLogQueue));
        while (Pending.TryDequeue(out var entry))
            queue.TryEnqueue(entry);
    }

    public static void CaptureError(string errorCode, string category, string message, Exception? exception = null)
    {
        Capture(ApplicationRuntimeLogSeverity.Error, errorCode, category, message, exception);
    }

    public static void CaptureWarning(string errorCode, string category, string message, Exception? exception = null)
    {
        Capture(ApplicationRuntimeLogSeverity.Warning, errorCode, category, message, exception);
    }

    private static void Capture(
        ApplicationRuntimeLogSeverity severity,
        string errorCode,
        string category,
        string message,
        Exception? exception)
    {
        if (string.IsNullOrWhiteSpace(errorCode) || string.IsNullOrWhiteSpace(message))
            return;

        var scrubbedMessage = ApplicationRuntimeLogTextHelper.ScrubSecrets(message);
        var entry = new ApplicationRuntimeLogEntry
        {
            OccurredAtUtc = DateTime.UtcNow,
            Severity = severity,
            Category = ApplicationRuntimeLogTextHelper.Truncate(category, 512),
            Message = ApplicationRuntimeLogTextHelper.Truncate(scrubbedMessage, 4000),
            ExceptionType = ApplicationRuntimeLogTextHelper.Truncate(exception?.GetType().FullName, 512),
            StackTrace = ApplicationRuntimeLogTextHelper.Truncate(
                ApplicationRuntimeLogTextHelper.ScrubSecrets(exception?.ToString()), 16000),
            ErrorCode = ApplicationRuntimeLogTextHelper.Truncate(errorCode.Trim(), 64),
            MachineName = ApplicationRuntimeLogTextHelper.Truncate(Environment.MachineName, 128),
            DeploymentEnvironment = ApplicationRuntimeLogEnvironmentHelper.DetectDeploymentEnvironment(),
            ApplicationVersion = ApplicationRuntimeLogTextHelper.Truncate(
                ApplicationRuntimeLogEnvironmentHelper.ResolveApplicationVersion(), 128)
        };

        if (queue != null)
            queue.TryEnqueue(entry);
        else
            Pending.Enqueue(entry);
    }
}

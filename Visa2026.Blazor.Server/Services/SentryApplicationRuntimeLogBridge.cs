using Microsoft.Extensions.Options;
using Sentry;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

internal sealed class SentryApplicationRuntimeLogBridge : IApplicationRuntimeLogSentryBridge
{
    private readonly IOptionsMonitor<SentryRuntimeOptions> optionsMonitor;

    public SentryApplicationRuntimeLogBridge(IOptionsMonitor<SentryRuntimeOptions> optionsMonitor)
    {
        this.optionsMonitor = optionsMonitor;
    }

    public string? TryCapture(ApplicationRuntimeLogEntry entry)
    {
        if (entry == null)
            return null;

        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.BridgeRuntimeLog || !SentrySdk.IsEnabled)
            return null;

        if (entry.Severity == ApplicationRuntimeLogSeverity.Warning && !options.BridgeWarnings)
            return null;

        var sentryEvent = BuildEvent(entry);
        var eventId = SentrySdk.CaptureEvent(sentryEvent);
        return eventId == SentryId.Empty ? null : eventId.ToString();
    }

    internal static SentryEvent BuildEvent(ApplicationRuntimeLogEntry entry)
    {
        var message = ApplicationRuntimeLogTextHelper.ScrubSecrets(entry.Message) ?? "(no message)";
        var sentryEvent = new SentryEvent
        {
            Message = message,
            Level = MapLevel(entry.Severity),
            Logger = entry.Category
        };

        if (!string.IsNullOrWhiteSpace(entry.ErrorCode))
            sentryEvent.SetTag("error_code", entry.ErrorCode.Trim());

        if (!string.IsNullOrWhiteSpace(entry.CorrelationId))
            sentryEvent.SetTag("correlation_id", entry.CorrelationId.Trim());

        sentryEvent.SetTag("deployment_environment", entry.DeploymentEnvironment.ToString());

        if (!string.IsNullOrWhiteSpace(entry.UserName))
            sentryEvent.User.Username = entry.UserName.Trim();

        if (!string.IsNullOrWhiteSpace(entry.RequestPath))
            sentryEvent.SetTag("request_path", entry.RequestPath.Trim());

        if (entry.RelatedBatchId is { } batchId && batchId != Guid.Empty)
            sentryEvent.SetTag("batch_id", batchId.ToString("D"));

        if (!string.IsNullOrWhiteSpace(entry.ExceptionType))
            sentryEvent.SetExtra("exception_type", entry.ExceptionType);

        if (!string.IsNullOrWhiteSpace(entry.StackTrace))
            sentryEvent.SetExtra("stack_trace", ApplicationRuntimeLogTextHelper.ScrubSecrets(entry.StackTrace));

        return sentryEvent;
    }

    private static SentryLevel MapLevel(ApplicationRuntimeLogSeverity severity) => severity switch
    {
        ApplicationRuntimeLogSeverity.Critical => SentryLevel.Fatal,
        ApplicationRuntimeLogSeverity.Error => SentryLevel.Error,
        ApplicationRuntimeLogSeverity.Warning => SentryLevel.Warning,
        _ => SentryLevel.Error
    };
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogBackgroundService : BackgroundService
{
    private readonly ApplicationRuntimeLogQueue queue;
    private readonly IApplicationRuntimeLogPersistence persistence;
    private readonly IApplicationRuntimeLogNotifier notifier;
    private readonly IApplicationRuntimeLogSentryBridge sentryBridge;
    private readonly IApplicationRuntimeLogCursorInboxWriter cursorInboxWriter;
    private readonly ILogger<ApplicationRuntimeLogBackgroundService> logger;

    public ApplicationRuntimeLogBackgroundService(
        ApplicationRuntimeLogQueue queue,
        IApplicationRuntimeLogPersistence persistence,
        IApplicationRuntimeLogNotifier notifier,
        IApplicationRuntimeLogSentryBridge sentryBridge,
        IApplicationRuntimeLogCursorInboxWriter cursorInboxWriter,
        ILogger<ApplicationRuntimeLogBackgroundService> logger)
    {
        this.queue = queue;
        this.persistence = persistence;
        this.notifier = notifier;
        this.sentryBridge = sentryBridge;
        this.cursorInboxWriter = cursorInboxWriter;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var entry in queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
        {
            try
            {
                var persistEntry = AttachSentryEventId(entry);
                var id = await persistence.PersistAsync(persistEntry, stoppingToken).ConfigureAwait(false);
                if (id.HasValue && id.Value != Guid.Empty)
                {
                    await cursorInboxWriter.TryWriteAsync(id.Value, persistEntry, stoppingToken)
                        .ConfigureAwait(false);

                    await notifier.NotifyAsync(
                        ApplicationRuntimeLogNotification.FromPersisted(id.Value, persistEntry),
                        stoppingToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "ApplicationRuntimeLog persistence skipped.");
            }
        }
    }

    private ApplicationRuntimeLogEntry AttachSentryEventId(ApplicationRuntimeLogEntry entry)
    {
        var sentryEventId = sentryBridge.TryCapture(entry);
        if (string.IsNullOrWhiteSpace(sentryEventId))
            return entry;

        return new ApplicationRuntimeLogEntry
        {
            OccurredAtUtc = entry.OccurredAtUtc,
            Severity = entry.Severity,
            Category = entry.Category,
            Message = entry.Message,
            ExceptionType = entry.ExceptionType,
            StackTrace = entry.StackTrace,
            ErrorCode = entry.ErrorCode,
            UserName = entry.UserName,
            CorrelationId = entry.CorrelationId,
            RequestPath = entry.RequestPath,
            MachineName = entry.MachineName,
            DeploymentEnvironment = entry.DeploymentEnvironment,
            ApplicationVersion = entry.ApplicationVersion,
            RelatedBatchId = entry.RelatedBatchId,
            SentryEventId = sentryEventId
        };
    }
}

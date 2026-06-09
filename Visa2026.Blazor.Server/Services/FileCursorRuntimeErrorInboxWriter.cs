using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Blazor.Server.Services;

public sealed class FileCursorRuntimeErrorInboxWriter : IApplicationRuntimeLogCursorInboxWriter
{
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;
    private readonly IHostEnvironment hostEnvironment;
    private readonly ILogger<FileCursorRuntimeErrorInboxWriter> logger;

    public FileCursorRuntimeErrorInboxWriter(
        IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor,
        IHostEnvironment hostEnvironment,
        ILogger<FileCursorRuntimeErrorInboxWriter> logger)
    {
        this.optionsMonitor = optionsMonitor;
        this.hostEnvironment = hostEnvironment;
        this.logger = logger;
    }

    public Task TryWriteAsync(Guid id, ApplicationRuntimeLogEntry entry, CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || !options.CursorBridgeEnabled)
            return Task.CompletedTask;

        if (options.CursorBridgeLocalDevOnly
            && entry.DeploymentEnvironment != ApplicationRuntimeLogDeploymentEnvironment.LocalVisualStudio)
        {
            return Task.CompletedTask;
        }

        if (!ShouldWrite(entry.Severity, options))
            return Task.CompletedTask;

        try
        {
            var inboxDirectory = ResolveInboxDirectory(options);
            var row = new Visa2026.Module.BusinessObjects.Operations.ApplicationRuntimeLog
            {
                ID = id,
                OccurredAtUtc = entry.OccurredAtUtc,
                Severity = entry.Severity,
                ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
                ErrorCode = entry.ErrorCode ?? string.Empty,
                Category = entry.Category ?? string.Empty,
                Message = entry.Message ?? string.Empty,
                ExceptionType = entry.ExceptionType ?? string.Empty,
                StackTrace = entry.StackTrace ?? string.Empty,
                UserName = entry.UserName ?? string.Empty,
                CorrelationId = entry.CorrelationId ?? string.Empty,
                RequestPath = entry.RequestPath ?? string.Empty,
                MachineName = entry.MachineName ?? string.Empty,
                DeploymentEnvironment = entry.DeploymentEnvironment,
                ApplicationVersion = entry.ApplicationVersion ?? string.Empty,
                RelatedBatchId = entry.RelatedBatchId,
                SentryEventId = entry.SentryEventId ?? string.Empty
            };

            if (ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                    row,
                    inboxDirectory,
                    skipIfExists: false,
                    sourceSlot: null,
                    sourceDatabase: null,
                    out var inboxFile))
            {
                logger.LogDebug("Cursor runtime error inbox wrote {InboxFile}.", inboxFile);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Cursor runtime error inbox write skipped.");
        }

        return Task.CompletedTask;
    }

    internal string ResolveInboxDirectory(ApplicationRuntimeLogOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.CursorBridgeInboxPath))
            return Path.GetFullPath(options.CursorBridgeInboxPath);

        var repoRoot = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, ".."));
        return Path.Combine(repoRoot, ".cursor", "runtime-errors", "inbox");
    }

    private static bool ShouldWrite(ApplicationRuntimeLogSeverity severity, ApplicationRuntimeLogOptions options)
    {
        var minSeverity = severity switch
        {
            ApplicationRuntimeLogSeverity.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
            ApplicationRuntimeLogSeverity.Error => Microsoft.Extensions.Logging.LogLevel.Error,
            ApplicationRuntimeLogSeverity.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
            _ => Microsoft.Extensions.Logging.LogLevel.Error
        };

        return minSeverity >= options.CursorBridgeMinLevel;
    }
}

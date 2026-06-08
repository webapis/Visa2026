using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class ApplicationRuntimeLogRetentionBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    private readonly IApplicationRuntimeLogRetention retention;
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;
    private readonly ILogger<ApplicationRuntimeLogRetentionBackgroundService> logger;

    public ApplicationRuntimeLogRetentionBackgroundService(
        IApplicationRuntimeLogRetention retention,
        IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor,
        ILogger<ApplicationRuntimeLogRetentionBackgroundService> logger)
    {
        this.retention = retention;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ApplicationRuntimeLogRetentionBackgroundService is starting.");

        try
        {
            await Task.Delay(StartupDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunPurgeCycleAsync(stoppingToken).ConfigureAwait(false);

            var intervalHours = optionsMonitor.CurrentValue.RetentionCleanupIntervalHours;
            if (intervalHours <= 0)
                intervalHours = 24;

            try
            {
                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunPurgeCycleAsync(CancellationToken cancellationToken)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || options.RetentionDays <= 0)
            return;

        try
        {
            int deleted = await retention.PurgeExpiredAsync(cancellationToken).ConfigureAwait(false);
            if (deleted > 0)
            {
                logger.LogInformation(
                    "ApplicationRuntimeLog retention removed {DeletedCount} row(s) older than {RetentionDays} day(s).",
                    deleted,
                    options.RetentionDays);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "ApplicationRuntimeLog retention purge failed.");
        }
    }
}

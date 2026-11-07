using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Visa2026.PortalFiling.Worker;

public sealed class PortalFilingWorkerHostedService : BackgroundService
{
    private readonly PortalFilingOptions _options;
    private readonly Visa2026ApiClient _api;
    private readonly PortalFormFiller _filler;
    private readonly ILogger<PortalFilingWorkerHostedService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public PortalFilingWorkerHostedService(
        IOptions<PortalFilingOptions> options,
        Visa2026ApiClient api,
        PortalFormFiller filler,
        ILogger<PortalFilingWorkerHostedService> logger,
        IHostApplicationLifetime lifetime)
    {
        _options = options.Value;
        _api = api;
        _filler = filler;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Portal filing worker starting. ScaffoldOnly={ScaffoldOnly}, PollSeconds={Poll}",
            _options.ScaffoldOnly,
            _options.PollSeconds);

        if (_options.ScaffoldOnly)
        {
            _logger.LogInformation(
                "Scaffold mode: config OK. Set PortalFiling:ScaffoldOnly=false and implement job API + selectors to process jobs.");
            _logger.LogInformation(
                "Visa2026 BaseUrl={BaseUrl}; Portal BaseUrl={Portal}",
                _options.Visa2026.BaseUrl,
                _options.Portal.BaseUrl);
            _lifetime.StopApplication();
            return;
        }

        await _api.AuthenticateAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _api.ClaimOldestReadyAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollSeconds)), stoppingToken);
                    continue;
                }

                await _api.UpdateStatusAsync(job.Id, FilingJobStatuses.Filling, cancellationToken: stoppingToken);
                var screenshot = await _filler.FillDraftAsync(job, stoppingToken);
                await _api.UpdateStatusAsync(
                    job.Id,
                    FilingJobStatuses.DraftOnScreen,
                    failureReason: screenshot is null ? "No screenshot" : null,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker loop error");
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollSeconds)), stoppingToken);
            }
        }
    }
}
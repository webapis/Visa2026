using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Visa2026.PortalFiling.Worker;

/// <summary>
/// Talks to Visa2026 Web API / future portal-filing job endpoints.
/// Scaffold: authenticate + claim/update are stubs until the job BO exists.
/// </summary>
public sealed class Visa2026ApiClient
{
    private readonly PortalFilingOptions _options;
    private readonly ILogger<Visa2026ApiClient> _logger;
    private readonly HttpClient _http;

    public Visa2026ApiClient(
        IOptions<PortalFilingOptions> options,
        ILogger<Visa2026ApiClient> logger,
        HttpClient http)
    {
        _options = options.Value;
        _logger = logger;
        _http = http;
        if (!string.IsNullOrWhiteSpace(_options.Visa2026.BaseUrl))
        {
            _http.BaseAddress = new Uri(_options.Visa2026.BaseUrl.TrimEnd('/') + "/");
        }
    }

    public Task<string?> AuthenticateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Scaffold: would POST /api/Authentication/Authenticate to {BaseUrl} as {User}",
            _options.Visa2026.BaseUrl,
            string.IsNullOrWhiteSpace(_options.Visa2026.UserName) ? "(not set)" : _options.Visa2026.UserName);
        return Task.FromResult<string?>(null);
    }

    public Task<FilingJobDto?> ClaimOldestReadyAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scaffold: no Visa2026 filing-job API yet — nothing to claim.");
        return Task.FromResult<FilingJobDto?>(null);
    }

    public Task UpdateStatusAsync(
        Guid jobId,
        string status,
        string? portalApplicationNumber = null,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Scaffold: would update job {JobId} → {Status} portal#={PortalNumber} fail={Fail}",
            jobId,
            status,
            portalApplicationNumber,
            failureReason);
        return Task.CompletedTask;
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class EfApplicationRuntimeLogRetention : IApplicationRuntimeLogRetention
{
    private readonly IConfiguration configuration;
    private readonly IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor;

    public EfApplicationRuntimeLogRetention(
        IConfiguration configuration,
        IOptionsMonitor<ApplicationRuntimeLogOptions> optionsMonitor)
    {
        this.configuration = configuration;
        this.optionsMonitor = optionsMonitor;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled)
            return 0;

        var cutoff = ApplicationRuntimeLogRetentionHelper.TryGetCutoffUtc(
            options.RetentionDays,
            DateTime.UtcNow);
        if (cutoff == null)
            return 0;

        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return 0;

        var batchSize = options.RetentionBatchSize <= 0
            ? ApplicationRuntimeLogRetentionHelper.DefaultBatchSize
            : options.RetentionBatchSize;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);

        int totalDeleted = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            int deleted = await dbContext.ApplicationRuntimeLogs
                .Where(row => row.OccurredAtUtc < cutoff.Value)
                .Take(batchSize)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            totalDeleted += deleted;
            if (deleted < batchSize)
                break;
        }

        return totalDeleted;
    }
}

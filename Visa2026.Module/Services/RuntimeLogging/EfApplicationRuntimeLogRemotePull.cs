using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class EfApplicationRuntimeLogRemotePull
{
    private readonly IConfiguration configuration;

    public EfApplicationRuntimeLogRemotePull(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<ApplicationRuntimeLogRemotePullResult> PullAsync(
        DateTime sinceUtc,
        int limit,
        string inboxDirectory,
        ApplicationRuntimeLogSeverity minSeverity,
        bool skipExisting,
        string? sourceSlot,
        string? sourceDatabase,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new ApplicationRuntimeLogRemotePullResult();
        }

        if (limit <= 0)
            limit = 50;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);
        var rows = await dbContext.ApplicationRuntimeLogs
            .AsNoTracking()
            .Where(x => x.GCRecord == 0)
            .Where(x => x.OccurredAtUtc >= sinceUtc)
            .Where(x => x.Severity >= minSeverity)
            .Where(x => x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.Open
                || x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.Acknowledged
                || x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.InProgress)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var writtenIds = new List<Guid>();
        int skipped = 0;

        foreach (var row in rows)
        {
            if (ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                    row,
                    inboxDirectory,
                    skipExisting,
                    sourceSlot,
                    sourceDatabase,
                    out _))
            {
                writtenIds.Add(row.ID);
            }
            else
            {
                skipped++;
            }
        }

        return new ApplicationRuntimeLogRemotePullResult
        {
            QueriedCount = rows.Count,
            WrittenCount = writtenIds.Count,
            SkippedCount = skipped,
            WrittenIds = writtenIds,
            NewestOccurredAtUtc = rows.Count > 0 ? rows[0].OccurredAtUtc : null
        };
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class EfApplicationRuntimeLogResolution : IApplicationRuntimeLogResolution
{
    private readonly IConfiguration configuration;

    public EfApplicationRuntimeLogResolution(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<bool> TryApplyAsync(RuntimeLogResolutionUpdate update, CancellationToken cancellationToken = default)
    {
        if (update == null)
            throw new ArgumentNullException(nameof(update));

        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return false;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);
        var row = await dbContext.ApplicationRuntimeLogs
            .FirstOrDefaultAsync(x => x.ID == update.Id, cancellationToken)
            .ConfigureAwait(false);
        if (row == null)
            return false;

        ApplicationRuntimeLogResolutionHelper.ApplyStatus(
            row,
            update.Status,
            DateTime.UtcNow,
            update.ResolvedBy,
            update.ResolutionNotes,
            update.FixCommitHash,
            update.AgentRunId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<ApplicationRuntimeLogResolutionSummary>> ListOpenAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return Array.Empty<ApplicationRuntimeLogResolutionSummary>();

        if (limit <= 0)
            limit = 20;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);
        var rows = await dbContext.ApplicationRuntimeLogs
            .AsNoTracking()
            .Where(x => x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.Open
                || x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.Acknowledged
                || x.ResolutionStatus == ApplicationRuntimeLogResolutionStatus.InProgress)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ConvertAll(ApplicationRuntimeLogResolutionHelper.ToSummary);
    }

    public async Task<ApplicationRuntimeLogResolutionSummary?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);
        var row = await dbContext.ApplicationRuntimeLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id, cancellationToken)
            .ConfigureAwait(false);

        return row == null ? null : ApplicationRuntimeLogResolutionHelper.ToSummary(row);
    }
}

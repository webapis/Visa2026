using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public sealed class EfApplicationRuntimeLogPersistence : IApplicationRuntimeLogPersistence
{
    private readonly IConfiguration configuration;

    public EfApplicationRuntimeLogPersistence(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public async Task<Guid?> PersistAsync(ApplicationRuntimeLogEntry entry, CancellationToken cancellationToken)
    {
        var connectionString = ApplicationRuntimeLogEnvironmentHelper.ResolveConnectionString(configuration);
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        await using var dbContext = new Visa2026EFCoreDbContext(optionsBuilder.Options);
        var row = MapRow(entry);
        dbContext.ApplicationRuntimeLogs.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row.ID;
    }

    internal static ApplicationRuntimeLog MapRow(ApplicationRuntimeLogEntry entry) =>
        new()
        {
            OccurredAtUtc = entry.OccurredAtUtc,
            Severity = entry.Severity,
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
            SentryEventId = entry.SentryEventId ?? string.Empty,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open
        };
}

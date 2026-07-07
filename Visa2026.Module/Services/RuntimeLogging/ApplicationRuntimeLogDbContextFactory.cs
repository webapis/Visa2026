using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.RuntimeLogging;

/// <summary>
/// Builds <see cref="Visa2026EFCoreDbContext"/> for runtime-log services outside XAF ObjectSpace.
/// Must match Blazor UseChangeTrackingProxies() — notification strategy requires proxies on BaseImpl types (e.g. FileData).
/// </summary>
internal static class ApplicationRuntimeLogDbContextFactory
{
    public static Visa2026EFCoreDbContext Create(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Visa2026EFCoreDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.CommandTimeout(180);
            sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
        optionsBuilder.UseChangeTrackingProxies();
        optionsBuilder.UseObjectSpaceLinkProxies();
        return new Visa2026EFCoreDbContext(optionsBuilder.Options);
    }
}
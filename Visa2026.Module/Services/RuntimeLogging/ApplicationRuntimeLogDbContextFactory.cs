using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

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
        DatabaseProviderDetector.ConfigureEfCore(optionsBuilder, connectionString);
        optionsBuilder.UseChangeTrackingProxies();
        optionsBuilder.UseObjectSpaceLinkProxies();
        return new Visa2026EFCoreDbContext(optionsBuilder.Options);
    }
}
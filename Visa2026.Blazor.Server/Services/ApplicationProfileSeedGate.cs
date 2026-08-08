using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Runs <see cref="ApplicationProfileSeedSync"/> after host DI is ready when ModuleUpdater was skipped.
/// </summary>
internal static class ApplicationProfileSeedGate
{
    public static void EnsureSynced(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        try
        {
            using var scope = services.CreateScope();
            var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = osFactory.CreateNonSecuredObjectSpace(typeof(ApplicationProfile));

            var result = ApplicationProfileSeedSync.Sync(objectSpace);

            logger?.LogInformation(
                "Application profile seed sync: created={Created}, updated={Updated}, backfilled={Backfilled}.",
                result.ProfilesCreated,
                result.ProfilesUpdated,
                result.ApplicationsBackfilled);

            if (result.TypesWithoutProfile.Count > 0)
            {
                logger?.LogWarning(
                    "Application profile seed: {Count} application type(s) had no profile mapping during backfill: {Types}",
                    result.TypesWithoutProfile.Count,
                    string.Join(", ", result.TypesWithoutProfile));
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Application profile seed sync failed.");
            throw;
        }
    }
}

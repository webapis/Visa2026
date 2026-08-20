using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Runs <see cref="ApplicationProfileSeedSync"/> after host DI is ready when ModuleUpdater was skipped.
/// Tenant catalog JSON is applied here too (not only on DB version bump).
/// Phase B approval-leg instance heal uses a separate ObjectSpace and never blocks startup.
/// </summary>
internal static class ApplicationProfileSeedGate
{
    public static void EnsureSynced(IServiceProvider services, ILogger? logger = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        try
        {
            var configuration = services.GetService<IConfiguration>();
            var connectionString = configuration?.GetConnectionString("DefaultConnection")
                ?? configuration?.GetConnectionString("ConnectionString");
            if (!PostgresRelationExists.All(connectionString, "ApplicationTypes", "ApplicationProfiles"))
            {
                logger?.LogInformation(
                    "ApplicationProfile seed skipped — ApplicationTypes schema not created yet (CheckCompatibility still pending).");
                return;
            }

            using var scope = services.CreateScope();
            var osFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            using var objectSpace = osFactory.CreateNonSecuredObjectSpace(typeof(ApplicationProfile));

            var result = ApplicationProfileSeedSync.Sync(objectSpace);

            logger?.LogInformation(
                "ApplicationProfile seed sync: created={Created}, updated={Updated}, backfilled={Backfilled}, tenantCatalog={TenantCatalog}.",
                result.ProfilesCreated,
                result.ProfilesUpdated,
                result.ApplicationsBackfilled,
                result.SkippedBecauseTenantCatalogPresent);

            if (result.TypesWithoutProfile.Count > 0)
            {
                logger?.LogWarning(
                    "ApplicationProfileInstance profile seed: {Count} application type(s) had no profile mapping during backfill: {Types}",
                    result.TypesWithoutProfile.Count,
                    string.Join(", ", result.TypesWithoutProfile));
            }

            // Fresh OS: catalog ObjectSpace still tracks profiles/versions from the sync above.
            RunApprovalLegInstanceHeal(osFactory, logger);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "ApplicationProfileInstance profile seed sync failed.");
            throw;
        }
    }

    private static void RunApprovalLegInstanceHeal(
        INonSecuredObjectSpaceFactory osFactory,
        ILogger? logger)
    {
        try
        {
            using var healSpace = osFactory.CreateNonSecuredObjectSpace(typeof(ApplicationProfileInstance));
            var heal = ApplicationProfileInstanceApprovalLegBackfill.Sync(healSpace);
            logger?.LogInformation(
                "ApplicationProfile approval-leg instance heal: scanned={Scanned}, assigned={Assigned}, names={Names}, snapshots={Snapshots}.",
                heal.Scanned,
                heal.ProfilesAssigned,
                heal.NamesStamped,
                heal.SnapshotsFilled);
        }
        catch (Exception ex)
        {
            // Do not block F5 / host start — officer can run CLI backfill.
            logger?.LogError(
                ex,
                "ApplicationProfile approval-leg instance heal failed. App will start. "
                + "Optional: dotnet run --project Visa2026.DataImporter -- --backfill-application-approval-leg-snapshots");
        }
    }
}

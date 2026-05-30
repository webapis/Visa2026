using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs lookup/reference rows from embedded <c>DatabaseUpdate/LookupCatalogs/*.json</c>.
/// Optional tenant overlay: <c>{AppDirectory}/LookupCatalogs/tenant/*.json</c>.
/// <see cref="ApplicationType"/> remains on <see cref="ApplicationTypeConfigurationUpdater"/>.
/// </summary>
public sealed class LookupCatalogSyncUpdater : ModuleUpdater
{
    public LookupCatalogSyncUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        LookupCatalogManifest manifest;
        try
        {
            manifest = LookupCatalogResourceLoader.LoadManifest();
        }
        catch (Exception ex)
        {
            var message = $"LookupCatalogSyncUpdater: failed to load manifest: {ex.Message}";
            Tracing.Tracer.LogError(message);
            Console.WriteLine(message);
            return;
        }

        int totalCreated = 0, totalUpdated = 0, totalSkipped = 0;
        int catalogsProcessed = 0;

        foreach (var definition in manifest.Catalogs)
        {
            var file = LookupCatalogResourceLoader.LoadCatalogFile(definition.File);
            if (file == null)
            {
                if (definition.Optional)
                    continue;

                Tracing.Tracer.LogText(
                    $"LookupCatalogSyncUpdater: catalog file '{definition.File}' not found for '{definition.Id}'.");
                continue;
            }

            try
            {
                catalogsProcessed++;
                var (created, updated, skipped) =
                    LookupCatalogEntitySync.Sync(ObjectSpace, definition, file);
                totalCreated += created;
                totalUpdated += updated;
                totalSkipped += skipped;
                var line =
                    $"LookupCatalogSyncUpdater: {definition.Id} ({definition.Entity}) "
                    + $"created={created}, updated={updated}, skipped={skipped}.";
                Tracing.Tracer.LogText(line);
                Console.WriteLine(line);
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message ?? ex.Message;
                var line =
                    $"LookupCatalogSyncUpdater: failed '{definition.Id}' ({definition.Entity}): {detail}";
                Tracing.Tracer.LogError(line);
                Console.WriteLine(line);
            }
        }

        if (totalCreated > 0 || totalUpdated > 0)
        {
            ObjectSpace.CommitChanges();
            var commitLine =
                $"LookupCatalogSyncUpdater: commit complete. totals created={totalCreated}, updated={totalUpdated}, skipped={totalSkipped}.";
            Tracing.Tracer.LogText(commitLine);
            Console.WriteLine(commitLine);
        }
        else if (catalogsProcessed > 0)
        {
            var warn =
                "LookupCatalogSyncUpdater: no rows created or updated — lookup tables unchanged. "
                + "If tables are empty, run a one-shot DB update (FORCE_XAF_DB_UPDATE=true or "
                + "Visa2026.Blazor.Server --updateDatabase --forceUpdate).";
            Tracing.Tracer.LogText(warn);
            Console.WriteLine(warn);
        }

        CleanupDuplicateOrganizationSingletons();

        var staleRemoved = LookupCatalogEntitySync.RemoveStaleOrganizationSingletonDuplicates(
            ObjectSpace, manifest.Catalogs);
        if (staleRemoved > 0)
        {
            ObjectSpace.CommitChanges();
            var pruneLine =
                $"LookupCatalogSyncUpdater: removed {staleRemoved} extra organization singleton row(s); one row per entity enforced.";
            Tracing.Tracer.LogText(pruneLine);
            Console.WriteLine(pruneLine);
        }
    }

    private void CleanupDuplicateOrganizationSingletons()
    {
        CleanupEmptyOrganizationSingletons<CompanyProfile>(p => p.Name, "CompanyProfile");
        CleanupEmptyOrganizationSingletons<ApplicationNumberingProfile>(p => p.Name, "ApplicationNumberingProfile");
        CleanupEmptyOrganizationSingletons<AuthorizedSignatory>(p => p.FullName, "AuthorizedSignatory");
        CleanupEmptyOrganizationSingletons<AuthorizedRepresentative>(p => p.FullName, "AuthorizedRepresentative");
    }

    private void CleanupEmptyOrganizationSingletons<T>(
        Func<T, string?> keySelector,
        string label) where T : class
    {
        var rows = ObjectSpace.GetObjects(typeof(T)).Cast<T>().ToList();
        var populated = rows.Where(p => !string.IsNullOrWhiteSpace(keySelector(p))).ToList();
        if (populated.Count == 0)
            return;

        int removed = 0;
        foreach (var empty in rows.Where(p => string.IsNullOrWhiteSpace(keySelector(p))))
        {
            ObjectSpace.Delete(empty);
            removed++;
        }

        if (removed <= 0)
            return;

        ObjectSpace.CommitChanges();
        var line = $"LookupCatalogSyncUpdater: removed {removed} empty {label} row(s) (singleton).";
        Tracing.Tracer.LogText(line);
        Console.WriteLine(line);
    }
}

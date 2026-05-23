using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
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
            Tracing.Tracer.LogError($"LookupCatalogSyncUpdater: failed to load manifest: {ex.Message}");
            return;
        }

        int totalCreated = 0, totalUpdated = 0, totalSkipped = 0;

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
                var (created, updated, skipped) =
                    LookupCatalogEntitySync.Sync(ObjectSpace, definition, file);
                totalCreated += created;
                totalUpdated += updated;
                totalSkipped += skipped;
                Tracing.Tracer.LogText(
                    $"LookupCatalogSyncUpdater: {definition.Id} ({definition.Entity}) "
                    + $"created={created}, updated={updated}, skipped={skipped}.");
            }
            catch (Exception ex)
            {
                Tracing.Tracer.LogError(
                    $"LookupCatalogSyncUpdater: failed '{definition.Id}' ({definition.Entity}): {ex.Message}");
            }
        }

        if (totalCreated > 0 || totalUpdated > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"LookupCatalogSyncUpdater: commit complete. totals created={totalCreated}, updated={totalUpdated}, skipped={totalSkipped}.");
        }
    }
}

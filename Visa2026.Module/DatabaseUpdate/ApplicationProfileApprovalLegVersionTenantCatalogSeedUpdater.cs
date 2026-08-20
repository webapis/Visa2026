using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs via-ministry template <see cref="ApplicationProfile.DefaultApprovalLegProfile"/>
/// from tenant <c>application-profile-approval-leg-versions*.json</c> (Phase A),
/// then Phase B instance snapshot / version-name backfill.
/// Runs after profile + ApprovalLegProfile seeders.
/// </summary>
public sealed class ApplicationProfileApprovalLegVersionTenantCatalogSeedUpdater : ModuleUpdater
{
    public ApplicationProfileApprovalLegVersionTenantCatalogSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        SyncNow(ObjectSpace);
    }

    public static void SyncNow(IObjectSpace objectSpace)
    {
        ApplicationProfileApprovalLegVersionTenantCatalogSync.Sync(objectSpace);
        objectSpace.CommitChanges();
        try
        {
            ApplicationProfileInstanceApprovalLegBackfill.Sync(objectSpace);
        }
        catch (Exception ex)
        {
            Tracing.Tracer.LogError(
                "ApplicationProfileApprovalLegVersionTenantCatalogSeedUpdater: instance heal failed (catalog Defaults were saved): "
                + ex.Message);
        }

        var count = objectSpace.GetObjectsQuery<ApplicationProfileApprovalLegVersion>().Count();
        Tracing.Tracer.LogText(
            $"ApplicationProfileApprovalLegVersionTenantCatalogSeedUpdater.SyncNow: versions in database={count}.");
    }

    public static int CountApprovedCatalogRows() =>
        ApplicationProfileApprovalLegVersionTenantCatalogLoader.TryLoadRows(out var rows) ? rows.Count : 0;
}
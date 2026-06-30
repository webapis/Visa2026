using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;

namespace Visa2026.Module.Services.MigrationImport;

/// <summary>
/// Disables <see cref="AuditTrailService"/> on Object Spaces created during DataImporter OData requests.
/// </summary>
internal static class MigrationImportAuditTrailObjectSpaceHooks
{
    internal static void ApplyIfNeeded(IObjectSpace objectSpace)
    {
        if (!MigrationImportContext.IsAuditTrailSuppressed)
            return;

        if (objectSpace is not EFCoreObjectSpace efCoreObjectSpace)
            return;

        efCoreObjectSpace.GetAuditTrailService().Enabled = false;
    }
}

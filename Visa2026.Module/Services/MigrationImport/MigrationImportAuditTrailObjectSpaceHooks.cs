using System.Runtime.CompilerServices;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;

namespace Visa2026.Module.Services.MigrationImport;

/// <summary>
/// Disables <see cref="AuditTrailService"/> on Object Spaces created during VISA2014 import
/// (OData with <c>X-Visa2014-DataImport</c> or headless in-process).
/// </summary>
internal static class MigrationImportAuditTrailObjectSpaceHooks
{
    private static readonly ConditionalWeakTable<IObjectSpace, object> Hooked = new();

    internal static void ApplyIfNeeded(IObjectSpace objectSpace)
    {
        if (!MigrationImportContext.IsAuditTrailSuppressed)
            return;

        DisableAuditTrail(objectSpace);

        if (Hooked.TryGetValue(objectSpace, out _))
            return;

        Hooked.Add(objectSpace, null);
        objectSpace.Committing += (_, _) => DisableAuditTrail(objectSpace);
    }

    private static void DisableAuditTrail(IObjectSpace objectSpace)
    {
        if (objectSpace is not EFCoreObjectSpace efCoreObjectSpace)
            return;

        efCoreObjectSpace.GetAuditTrailService().Enabled = false;
    }
}

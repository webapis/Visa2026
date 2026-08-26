using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Officer UI: new issued work permits must be created from an <see cref="ApplicationProfileInstance"/>
/// (Issued records) so <see cref="WorkPermit.ApplicationProfileInstance"/> is authoritative.
/// </summary>
public static class WorkPermitIssuingOriginPolicy
{
    public static bool RequiresApplicationProfileInstanceOnSave(WorkPermit? workPermit)
    {
        if (workPermit == null)
            return false;

        if (MigrationImportContext.IsDataImport)
            return false;

        var objectSpace = ObjectSpaceHelper.Get(workPermit);
        if (objectSpace == null || !objectSpace.IsNewObject(workPermit))
            return false;

        return true;
    }

    public static bool HasRequiredApplicationProfileInstance(WorkPermit? workPermit)
    {
        if (!RequiresApplicationProfileInstanceOnSave(workPermit))
            return true;

        return workPermit!.ApplicationProfileInstance != null;
    }
}

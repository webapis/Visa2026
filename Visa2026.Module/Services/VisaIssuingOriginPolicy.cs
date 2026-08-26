using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Officer UI: new issued visas must be created from an <see cref="ApplicationProfileInstance"/>
/// (Issued records) so <see cref="Visa.IssuingApplicationProfileInstance"/> is authoritative.
/// </summary>
public static class VisaIssuingOriginPolicy
{
    public static bool RequiresIssuingApplicationProfileInstanceOnSave(Visa? visa)
    {
        if (visa == null)
            return false;

        if (MigrationImportContext.IsDataImport)
            return false;

        var objectSpace = ObjectSpaceHelper.Get(visa);
        if (objectSpace == null || !objectSpace.IsNewObject(visa))
            return false;

        return true;
    }

    public static bool HasRequiredIssuingApplicationProfileInstance(Visa? visa)
    {
        if (!RequiresIssuingApplicationProfileInstanceOnSave(visa))
            return true;

        return visa!.IssuingApplicationProfileInstance != null;
    }
}

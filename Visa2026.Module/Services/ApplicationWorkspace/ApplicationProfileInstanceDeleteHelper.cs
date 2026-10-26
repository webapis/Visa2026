using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Officers may delete an <see cref="ApplicationProfileInstance"/> from its case workspace only when the case
/// has no issued records. Issued invitations, visas, work permits, border zones and rejections are aggregated
/// on the instance, so deleting the case would delete them too; officers remove those first under Application result.
/// </summary>
public static class ApplicationProfileInstanceDeleteHelper
{
    public static int CountIssuedRecords(IObjectSpace objectSpace, Guid instanceId)
    {
        if (objectSpace == null || instanceId == Guid.Empty)
            return 0;

        return objectSpace.GetObjectsQuery<Invitation>().Count(i => i.ApplicationProfileInstance != null && i.ApplicationProfileInstance.ID == instanceId)
            + objectSpace.GetObjectsQuery<Visa>().Count(v => v.IssuingApplicationProfileInstance != null && v.IssuingApplicationProfileInstance.ID == instanceId)
            + objectSpace.GetObjectsQuery<WorkPermit>().Count(w => w.ApplicationProfileInstance != null && w.ApplicationProfileInstance.ID == instanceId)
            + objectSpace.GetObjectsQuery<BorderZone>().Count(b => b.ApplicationProfileInstance != null && b.ApplicationProfileInstance.ID == instanceId)
            + objectSpace.GetObjectsQuery<Rejection>().Count(r => r.ApplicationProfileInstance != null && r.ApplicationProfileInstance.ID == instanceId);
    }

    public static bool TryDelete(IObjectSpace objectSpace, Guid instanceId, out string error)
    {
        error = string.Empty;
        var instance = objectSpace == null || instanceId == Guid.Empty
            ? null
            : objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.DeleteCaseNotFound");
            return false;
        }

        var issued = CountIssuedRecords(objectSpace!, instanceId);
        if (issued > 0)
        {
            error = VisaUiMessages.Format("ApplicationProfileInstance.Workspace.DeleteCaseHasIssuedRecords", issued);
            return false;
        }

        objectSpace!.Delete(instance);
        objectSpace.CommitChanges();
        return true;
    }
}

using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

/// <summary>
/// Officers may delete an Application Profile template only when no
/// <see cref="ApplicationProfileInstance"/> points at it.
/// </summary>
public static class ApplicationProfileUnlinkedDeleteHelper
{
    public const string LinkedMessage =
        "Cannot delete this Application Profile template because it is linked to Application Profile Instance(s).";

    public const string NotFoundMessage = "Application Profile was not found.";

    public static bool CanDelete(int linkedInstanceCount) => linkedInstanceCount <= 0;

    public static int CountLinkedInstances(IObjectSpace objectSpace, Guid profileId)
    {
        if (objectSpace == null || profileId == Guid.Empty)
            return 0;

        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Count(a => a.ApplicationProfile != null && a.ApplicationProfile.ID == profileId);
    }

    public static bool TryDelete(IObjectSpace objectSpace, Guid profileId, out string error)
    {
        error = string.Empty;
        if (objectSpace == null || profileId == Guid.Empty)
        {
            error = NotFoundMessage;
            return false;
        }

        var linked = CountLinkedInstances(objectSpace, profileId);
        if (!CanDelete(linked))
        {
            error = LinkedMessage;
            return false;
        }

        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId);
        if (profile == null)
        {
            error = NotFoundMessage;
            return false;
        }

        objectSpace.Delete(profile);
        objectSpace.CommitChanges();
        return true;
    }
}
using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerApplyHelper
{
    public static void ApplyProfileToNewApplication(
        IObjectSpace objectSpace,
        Application application,
        ApplicationProfile profile,
        ApplicationProgressRouteKind? creationProgressRoute = null)
    {
        if (objectSpace == null || application == null || profile == null)
            return;

        var resolvedProfile = objectSpace.GetObject(profile);
        application.ApplicationProfile = resolvedProfile;

        if (creationProgressRoute.HasValue)
            application.CreationProgressRoute = creationProgressRoute;
        else if (!application.CreationProgressRoute.HasValue)
            application.CreationProgressRoute = resolvedProfile.ProgressRoute;

        var matchingType = FindMatchingApplicationType(objectSpace, resolvedProfile);
        // Dual-write Type FK for legacy SQL/report paths until slice 13b drops the column.
        if (matchingType != null)
            application.ApplicationType = matchingType;

        objectSpace.SetModified(application);
    }

    public static ApplicationType? FindMatchingApplicationType(IObjectSpace objectSpace, ApplicationProfile profile)
    {
        if (objectSpace == null || profile == null || string.IsNullOrWhiteSpace(profile.Code))
            return null;

        return objectSpace.GetObjectsQuery<ApplicationType>()
            .AsEnumerable()
            .FirstOrDefault(type => string.Equals(
                ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type),
                profile.Code,
                StringComparison.OrdinalIgnoreCase));
    }
}

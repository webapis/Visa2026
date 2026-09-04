using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Keeps denormalized latest-progress list fields on <see cref="Application"/> in sync with <see cref="ApplicationProfileInstanceProgress"/> history.
/// </summary>
public static class ApplicationLatestProgressSyncHelper
{
    public static ApplicationProfileInstanceProgress? ResolveLatestForDisplay(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        if (application.LatestProgress != null && application.LatestProgressId == application.LatestProgress.ID)
            return application.LatestProgress;

        if (application.LatestProgressId != null)
        {
            if (application.ProgressHistory != null)
            {
                foreach (var progress in application.ProgressHistory)
                {
                    if (progress.ID == application.LatestProgressId)
                        return progress;
                }
            }

            var objectSpace = ObjectSpaceHelper.Get(application);
            if (objectSpace != null)
                return objectSpace.GetObjectByKey<ApplicationProfileInstanceProgress>(application.LatestProgressId.Value);
        }

        return ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory);
    }

    public static void Sync(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null)
    {
        if (application == null)
            return;

        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        Apply(application, latest, objectSpace);
        application.InvalidateListViewDisplayCache();
    }

    public static void Apply(ApplicationProfileInstance application, ApplicationProfileInstanceProgress? latest, IObjectSpace? objectSpace = null)
    {
        if (latest == null)
        {
            application.LatestProgressId = null;
            application.LatestProgress = null;
            application.LatestPrimaryStateCode = null;
            application.LatestProgressDisplay = null;
            SyncProcessNumber(application, objectSpace);
            return;
        }

        var primaryCode = ApplicationProfileInstanceProgressPrimaryStateCodeResolver.ResolveFromLatest(latest) ?? string.Empty;
        application.LatestPrimaryStateCode = primaryCode;
        application.LatestProgressDisplay =
            ApplicationProfileInstanceProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(latest) ?? string.Empty;
        SyncProcessNumber(application, objectSpace);

        if (CanLinkLatestProgress(latest, objectSpace))
        {
            application.LatestProgressId = latest.ID;
            application.LatestProgress = latest;
            return;
        }

        application.LatestProgressId = null;
        application.LatestProgress = null;
    }

    /// <summary>
    /// Keeps denormalized <see cref="Application.ProcessNumber"/> aligned with progress history.
    /// </summary>
    public static void SyncProcessNumber(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null)
    {
        if (application == null)
            return;

        IEnumerable<ApplicationProfileInstanceProgress>? history = application.ProgressHistory;
        if (objectSpace != null && history != null)
            history = history.Where(p => !objectSpace.IsObjectToDelete(p));

        application.ProcessNumber = ApplicationProcessNumberHelper.ResolveFromHistory(history)
            ?? application.ProcessNumber;
    }

    /// <summary>
    /// EF cannot insert ApplicationProfileInstance and ApplicationProfileInstanceProgress in one batch when both are new and
    /// <see cref="Application.LatestProgressId"/> points at the child row (circular FK graph).
    /// Scalars are still updated; the pointer is linked after the first commit.
    /// </summary>
    private static bool CanLinkLatestProgress(ApplicationProfileInstanceProgress latest, IObjectSpace? objectSpace)
    {
        if (latest.ID == Guid.Empty)
            return false;

        return objectSpace == null || !objectSpace.IsNewObject(latest);
    }
}
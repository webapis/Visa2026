using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Keeps denormalized latest-progress list fields on <see cref="Application"/> in sync with <see cref="ApplicationProgress"/> history.
/// </summary>
public static class ApplicationLatestProgressSyncHelper
{
    public static ApplicationProgress? ResolveLatestForDisplay(Application? application)
    {
        if (application == null)
            return null;

        if (application.LatestProgress != null && application.LatestProgressId == application.LatestProgress.ID)
            return application.LatestProgress;

        if (application.LatestProgressId != null && application.ProgressHistory != null)
        {
            foreach (var progress in application.ProgressHistory)
            {
                if (progress.ID == application.LatestProgressId)
                    return progress;
            }
        }

        return ApplicationProgressHelper.GetLatest(application.ProgressHistory);
    }

    public static void Sync(Application? application, IObjectSpace? objectSpace = null)
    {
        if (application == null)
            return;

        var latest = ApplicationProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        Apply(application, latest, objectSpace);
        application.InvalidateListViewDisplayCache();
    }

    public static void Apply(Application application, ApplicationProgress? latest, IObjectSpace? objectSpace = null)
    {
        if (latest == null)
        {
            application.LatestProgressId = null;
            application.LatestProgress = null;
            application.LatestPrimaryStateCode = null;
            application.LatestProgressDisplay = null;
            return;
        }

        var primaryCode = ApplicationProgressPrimaryStateCodeResolver.ResolveFromLatest(latest) ?? string.Empty;
        application.LatestPrimaryStateCode = primaryCode;
        application.LatestProgressDisplay =
            ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayNameFromLatest(latest) ?? string.Empty;

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
    /// EF cannot insert Application and ApplicationProgress in one batch when both are new and
    /// <see cref="Application.LatestProgressId"/> points at the child row (circular FK graph).
    /// Scalars are still updated; the pointer is linked after the first commit.
    /// </summary>
    private static bool CanLinkLatestProgress(ApplicationProgress latest, IObjectSpace? objectSpace)
    {
        if (latest.ID == Guid.Empty)
            return false;

        return objectSpace == null || !objectSpace.IsNewObject(latest);
    }
}
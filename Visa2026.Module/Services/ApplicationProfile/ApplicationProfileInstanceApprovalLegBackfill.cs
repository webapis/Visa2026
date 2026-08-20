using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Phase B: stamp imported via-ministry instances with the shared
/// <see cref="ApprovalLegProfile"/> they already inferred (or the template Default)
/// and fill missing <see cref="ApplicationProfileInstanceApprovalLegSnapshot"/> rows.
/// Does not overwrite an instance FK that is already set (per-app chain from VISA2015).
/// Does not soft-delete existing snapshots (that trips OptimisticLockField on bulk heal).
/// </summary>
public static class ApplicationProfileInstanceApprovalLegBackfill
{
    public const int DefaultBatchSize = 75;

    public readonly record struct Result(
        int Scanned,
        int ProfilesAssigned,
        int NamesStamped,
        int SnapshotsFilled);

    public static Result Sync(IObjectSpace objectSpace, bool apply = true, int batchSize = DefaultBatchSize)
    {
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (batchSize < 1)
            batchSize = DefaultBatchSize;

        var via = ApplicationProfileInstanceProgressRouteKind.ViaMinistries;
        // CreationProgressRoute is [NotMapped] — do not filter on it in SQL.
        var ids = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a =>
                (a.ApplicationProfile != null && a.ApplicationProfile.ProgressRoute == via)
                || (a.ApplicationType != null && a.ApplicationType.ApplicationProfileInstanceProgressRoute == via))
            .Select(a => a.ID)
            .ToList();

        int scanned = 0, assigned = 0, stamped = 0, filled = 0;

        for (var offset = 0; offset < ids.Count; offset += batchSize)
        {
            var batchIds = ids.Skip(offset).Take(batchSize).ToList();
            var candidates = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                .Include(a => a.ApprovalLegProfile!)
                    .ThenInclude(p => p.MinistryLegs)
                        .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApplicationProfile!)
                    .ThenInclude(p => p!.DefaultApprovalLegProfile!)
                        .ThenInclude(d => d!.MinistryLegs)
                            .ThenInclude(l => l.ApprovingMinistry)
                .Include(a => a.ApplicationType)
                .Include(a => a.ApprovalLegSnapshots)
                .Where(a => batchIds.Contains(a.ID))
                .AsSplitQuery()
                .ToList();

            var batchDirty = false;
            foreach (var application in candidates)
            {
                if (!IsViaMinistry(application))
                    continue;

                scanned++;
                var outcome = Evaluate(application);
                if (outcome.Shared == null
                    || (!outcome.AssignProfile && !outcome.StampName && !outcome.FillSnapshots))
                    continue;

                try
                {
                    if (apply)
                    {
                        Apply(objectSpace, application, outcome);
                        batchDirty = true;
                    }
                }
                catch (Exception ex)
                {
                    Tracing.Tracer.LogError(
                        $"ApplicationProfileInstanceApprovalLegBackfill {application.FullApplicationNumber ?? application.ID.ToString()}: {ex.Message}");
                    continue;
                }

                if (outcome.AssignProfile)
                    assigned++;
                if (outcome.StampName)
                    stamped++;
                if (outcome.FillSnapshots)
                    filled++;
            }

            if (apply && batchDirty)
            {
                try
                {
                    objectSpace.CommitChanges();
                }
                catch (Exception ex)
                {
                    Tracing.Tracer.LogError(
                        $"ApplicationProfileInstanceApprovalLegBackfill batch commit failed at offset {offset}: {ex.Message}");
                    throw;
                }
            }
        }

        Tracing.Tracer.LogText(
            $"ApplicationProfileInstanceApprovalLegBackfill: scanned={scanned}, assigned={assigned}, names={stamped}, snapshots={filled}, apply={apply}.");

        return new Result(scanned, assigned, stamped, filled);
    }

    public static bool IsViaMinistry(ApplicationProfileInstance? application) =>
        ApplicationProfileConfigurationResolver.GetProgressRoute(application)
        == ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

    public static ApprovalLegProfile? ResolveShared(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        return application.ApprovalLegProfile
            ?? application.ApplicationProfile?.DefaultApprovalLegProfile;
    }

    internal static HealPlan Evaluate(ApplicationProfileInstance application)
    {
        var shared = ResolveShared(application);
        if (shared == null)
            return default;

        var assign = application.ApprovalLegProfile == null;
        var stamp = string.IsNullOrWhiteSpace(application.ApprovalLegVersionName);
        var fill = NeedsSnapshot(application, shared);
        return new HealPlan(shared, assign, stamp, fill);
    }

    /// <summary>
    /// Bulk heal only creates rows when the instance has no snapshot rows at all.
    /// Mismatched counts are left alone — recreating via soft-delete causes concurrency failures.
    /// </summary>
    public static bool NeedsSnapshot(ApplicationProfileInstance application, ApprovalLegProfile shared)
    {
        if (ApprovalLegProfileMinistryHelper.GetLegCount(shared) <= 0)
            return false;

        return (application.ApprovalLegSnapshots?.Count ?? 0) == 0;
    }

    public static string FormatVersionName(ApprovalLegProfile shared)
    {
        if (!string.IsNullOrWhiteSpace(shared.NameTm))
            return shared.NameTm.Trim();
        return shared.Code?.Trim() ?? string.Empty;
    }

    private static void Apply(IObjectSpace objectSpace, ApplicationProfileInstance application, HealPlan plan)
    {
        var shared = objectSpace.GetObject(plan.Shared);

        if (plan.AssignProfile)
            application.ApprovalLegProfile = shared;

        if (plan.StampName)
            application.ApprovalLegVersionName = FormatVersionName(shared);

        if (plan.FillSnapshots)
            AppendSnapshotsFromShared(objectSpace, application, shared);
    }

    /// <summary>
    /// Insert snapshot rows only. Never soft-deletes existing children (OptimisticLockField).
    /// </summary>
    private static void AppendSnapshotsFromShared(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApprovalLegProfile shared)
    {
        if (application.ApprovalLegSnapshots == null)
            application.ApprovalLegSnapshots =
                new System.Collections.ObjectModel.ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>();

        if (application.ApprovalLegSnapshots.Count > 0)
            return;

        foreach (var leg in shared.MinistryLegs
                     .Where(l => l.ApprovingMinistry != null)
                     .OrderBy(l => l.Sequence))
        {
            var snapshot = objectSpace.CreateObject<ApplicationProfileInstanceApprovalLegSnapshot>();
            snapshot.ApplicationProfileInstance = application;
            snapshot.Sequence = leg.Sequence;
            snapshot.ApprovingMinistryId = leg.ApprovingMinistry.ID;
            snapshot.MinistryShortName = leg.ApprovingMinistry.ShortNameTm ?? leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MinistryNameTm = leg.ApprovingMinistry.NameTm ?? string.Empty;
            if (MinistryReviewSlaHelper.TryGetEffectiveSla(objectSpace, out var maxDays, out var warningDays))
            {
                snapshot.MaxDaysInReview = maxDays;
                snapshot.WarningDaysBeforeMax = warningDays;
            }

            application.ApprovalLegSnapshots.Add(snapshot);
        }
    }

    internal readonly record struct HealPlan(
        ApprovalLegProfile? Shared,
        bool AssignProfile,
        bool StampName,
        bool FillSnapshots);
}

using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Removes duplicate <see cref="Urgency"/> rows (legacy seed created extra TOP/NORM/URG rows) and repoints <see cref="Application"/> FKs.
/// </summary>
public sealed class UrgencyDuplicateCleanupUpdater : ModuleUpdater
{
    public UrgencyDuplicateCleanupUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var all = ObjectSpace.GetObjectsQuery<Urgency>().ToList();
        var duplicateGroups = all
            .Where(u => !string.IsNullOrWhiteSpace(u.Code))
            .GroupBy(u => u.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateGroups.Count == 0)
            return;

        int deleted = 0;
        int repointed = 0;

        foreach (var group in duplicateGroups)
        {
            var keeper = SelectKeeper(group);
            foreach (var duplicate in group.Where(u => u.ID != keeper.ID))
            {
                repointed += RepointApplications(duplicate, keeper);
                ObjectSpace.Delete(duplicate);
                deleted++;
            }
        }

        if (deleted > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"UrgencyDuplicateCleanupUpdater: deleted {deleted} duplicate urgency row(s), repointed {repointed} application(s).");
        }
    }

    private static Urgency SelectKeeper(IGrouping<string, Urgency> group) =>
        group
            .OrderByDescending(u => u.IsDefault)
            .ThenBy(u => u.ID)
            .First();

    private int RepointApplications(Urgency from, Urgency to)
    {
        var applications = ObjectSpace.GetObjectsQuery<Application>()
            .Where(a => a.Urgency != null && a.Urgency.ID == from.ID)
            .ToList();

        foreach (var application in applications)
            application.Urgency = to;

        return applications.Count;
    }
}

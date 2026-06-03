using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// One-time (idempotent) sync of registration <see cref="ApplicationItem"/> travel fields into
/// linked <see cref="TravelHistory"/> rows after <see cref="TravelHistory.SourceApplicationItemID"/> is deployed.
/// </summary>
public sealed class RegistrationTravelHistoryBackfillUpdater : ModuleUpdater
{
    public RegistrationTravelHistoryBackfillUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var syncTypeNames = new[]
        {
            RegistrationTravelHistorySyncService.AppRegCheckIn,
            RegistrationTravelHistorySyncService.AppRegCheckOut,
            RegistrationTravelHistorySyncService.AppRegCheckInInternal,
            RegistrationTravelHistorySyncService.AppRegCheckOutInternal
        };

        var items = ObjectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai =>
                ai.Application != null
                && ai.Application.ApplicationType != null
                && syncTypeNames.Contains(ai.Application.ApplicationType.Name))
            .ToList();

        int synced = 0;
        int skipped = 0;

        foreach (var item in items)
        {
            try
            {
                var before = ObjectSpace.GetObjectsQuery<TravelHistory>()
                    .Count(th => th.SourceApplicationItemID == item.ID);
                RegistrationTravelHistorySyncService.SyncFromApplicationItem(item);
                var after = ObjectSpace.GetObjectsQuery<TravelHistory>()
                    .Count(th => th.SourceApplicationItemID == item.ID);
                if (after > 0 || before > 0)
                    synced++;
                else
                    skipped++;
            }
            catch (Exception ex)
            {
                skipped++;
                Tracing.Tracer.LogError(
                    $"RegistrationTravelHistoryBackfillUpdater: item {item.ID}: {ex.Message}");
            }
        }

        if (synced > 0)
            ObjectSpace.CommitChanges();

        var line =
            $"RegistrationTravelHistoryBackfillUpdater: items={items.Count}, synced={synced}, skipped={skipped}.";
        Tracing.Tracer.LogText(line);
        Console.WriteLine(line);
    }
}

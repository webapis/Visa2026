using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// One-shot: collapse duplicate AddressOfResidence rows by shared-catalog dedupe key,
/// repoint Case Address1/2 and People ResolvedLinks, clear obsolete ownership columns.
/// </summary>
public sealed class AddressOfResidenceCatalogDedupeUpdater : ModuleUpdater
{
    public AddressOfResidenceCatalogDedupeUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        var rows = ObjectSpace.GetObjectsQuery<AddressOfResidence>().ToList();
        if (rows.Count == 0)
            return;

        var groups = rows
            .Select(row => (row, key: AddressOfResidenceCatalogHelper.BuildDedupKey(row)))
            .Where(x => !string.IsNullOrWhiteSpace(x.key))
            .GroupBy(x => x.key, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .ToList();

        int merged = 0;
        foreach (var group in groups)
        {
            var ordered = group.Select(x => x.row).OrderBy(r => r.ID).ToList();
            var keeper = ordered[0];
            foreach (var duplicate in ordered.Skip(1))
            {
                RepointAddressReferences(keeper, duplicate);
                ObjectSpace.Delete(duplicate);
                merged++;
            }
        }

        foreach (var row in ObjectSpace.GetObjectsQuery<AddressOfResidence>().ToList())
            AddressOfResidenceCatalogHelper.ClearOwnership(row);

        if (merged > 0 || rows.Count > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"AddressOfResidenceCatalogDedupeUpdater: merged={merged}, cleared ownership on catalog rows.");
        }
    }

    private void RepointAddressReferences(AddressOfResidence keeper, AddressOfResidence duplicate)
    {
        var fromId = duplicate.ID;
        var toId = keeper.ID;
        if (fromId == Guid.Empty || toId == Guid.Empty || fromId == toId)
            return;

        foreach (var instance in ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                     .Where(i => i.Address1 != null && i.Address1.ID == fromId))
        {
            instance.Address1 = keeper;
        }

        foreach (var instance in ObjectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                     .Where(i => i.Address2 != null && i.Address2.ID == fromId))
        {
            instance.Address2 = keeper;
        }

        foreach (var link in ObjectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
                     .Where(l => l.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                         && l.LinkedObjectId == fromId))
        {
            link.LinkedObjectId = toId;
        }
    }
}
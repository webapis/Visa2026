using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Backfills empty <see cref="LookupBase.NameTm"/> from legacy <see cref="LookupBase.Name"/> on tenant and global lookup rows.</summary>
public sealed class LookupBaseNameTmBackfillUpdater : ModuleUpdater
{
    public LookupBaseNameTmBackfillUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        int updated = 0;
        foreach (var lookupType in TenantLookupTypes.All.Concat(LocalizedLookupTypes.All).Distinct())
        {
            updated += BackfillNameTm(lookupType);
        }

        if (updated > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText($"LookupBaseNameTmBackfillUpdater: set NameTm on {updated} lookup row(s).");
        }
    }

    private int BackfillNameTm(Type lookupType)
    {
        int updated = 0;
        foreach (LookupBase row in ObjectSpace.GetObjects(lookupType))
        {
            if (!string.IsNullOrWhiteSpace(row.NameTm))
                continue;
            if (string.IsNullOrWhiteSpace(row.Name))
                continue;

            row.NameTm = row.Name.Trim();
            updated++;
        }

        return updated;
    }
}

using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Backfills <see cref="LookupBase.LocalizationKey"/> from <see cref="LookupBase.Code"/> for global catalog rows.
/// </summary>
public sealed class LookupLocalizationKeyUpdater : ModuleUpdater
{
    public LookupLocalizationKeyUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        int updated = 0;
        foreach (var lookupType in LocalizedLookupTypes.All)
        {
            foreach (LookupBase row in ObjectSpace.GetObjects(lookupType))
            {
                var key = ResolveLocalizationKey(row);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (string.Equals(row.LocalizationKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                row.LocalizationKey = key;
                updated++;
            }
        }

        if (updated > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText($"LookupLocalizationKeyUpdater: set LocalizationKey on {updated} global lookup row(s).");
        }
    }

    private static string ResolveLocalizationKey(LookupBase row) => LookupLocalizationKeys.Resolve(row);
}

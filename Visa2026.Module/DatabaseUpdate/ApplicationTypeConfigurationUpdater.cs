using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Syncs <see cref="ApplicationType"/> rows from <see cref="ApplicationTypeConfigurationSeed"/>.
/// On every deploy: all <c>Show*</c> flags are overwritten from seed. Other fields are updated when seed supplies values.
/// Creates missing types by <see cref="ApplicationType.Name"/>.
/// </summary>
public sealed class ApplicationTypeConfigurationUpdater : ModuleUpdater
{
    public ApplicationTypeConfigurationUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        int updated = 0, created = 0;

        foreach (var row in ApplicationTypeConfigurationSeed.Rows)
        {
            var applicationType = ObjectSpace.GetObjectsQuery<ApplicationType>()
                .FirstOrDefault(t => t.Name == row.Name);

            if (applicationType == null)
            {
                applicationType = ObjectSpace.CreateObject<ApplicationType>();
                applicationType.Name = row.Name;
                created++;
            }
            else
            {
                updated++;
            }

            ApplicationTypeConfigurationApplier.Apply(applicationType, row, overwriteShowFlags: true);

            if (ApplicationTypeSelectionCodeSeed.TryGetByName(row.Name, out var selectionCode)
                && !string.IsNullOrWhiteSpace(selectionCode))
            {
                applicationType.SelectionCode = selectionCode;
            }
        }

        ObjectSpace.CommitChanges();

        if (created > 0 || updated > 0)
        {
            Tracing.Tracer.LogText(
                $"ApplicationTypeConfigurationUpdater: synced {ApplicationTypeConfigurationSeed.Rows.Count} seed row(s) "
                + $"(created={created}, updated={updated}); Show* flags overwritten from seed.");
        }

        var seedNames = ApplicationTypeConfigurationSeed.Rows
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var dbOnly = ObjectSpace.GetObjectsQuery<ApplicationType>()
            .Where(t => !seedNames.Contains(t.Name))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();
        if (dbOnly.Count > 0)
        {
            Tracing.Tracer.LogText(
                "ApplicationTypeConfigurationUpdater: ApplicationType rows in DB but not in seed (unchanged): "
                + string.Join(", ", dbOnly));
        }
    }
}

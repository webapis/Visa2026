using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Ensures <see cref="ApplicationType.SelectionCode"/> is populated from <see cref="ApplicationTypeSelectionCodeSeed"/>.
/// Administrators maintain codes on <see cref="ApplicationType"/> lookup rows after deploy.
/// </summary>
public sealed class ApplicationTypeSelectionCodeUpdater : ModuleUpdater
{
    public ApplicationTypeSelectionCodeUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();

        int seeded = 0;
        foreach (var applicationType in ObjectSpace.GetObjectsQuery<ApplicationType>())
        {
            if (!string.IsNullOrWhiteSpace(applicationType.SelectionCode))
                continue;

            if (!ApplicationTypeSelectionCodeSeed.TryGetByName(applicationType.Name, out var code))
                continue;

            applicationType.SelectionCode = code;
            seeded++;
        }

        if (seeded > 0)
        {
            ObjectSpace.CommitChanges();
            Tracing.Tracer.LogText(
                $"ApplicationTypeSelectionCodeUpdater: set SelectionCode on {seeded} application type(s).");
        }

        var missing = ObjectSpace.GetObjectsQuery<ApplicationType>()
            .Where(t => string.IsNullOrEmpty(t.SelectionCode))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        if (missing.Count > 0)
        {
            Tracing.Tracer.LogText(
                "ApplicationTypeSelectionCodeUpdater: ApplicationType rows still without SelectionCode: "
                + string.Join(", ", missing));
        }
    }
}

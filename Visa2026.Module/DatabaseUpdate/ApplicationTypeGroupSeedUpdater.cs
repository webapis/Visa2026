using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>Seeds <see cref="BusinessObjects.ApplicationTypeGroup"/> Registration membership before user-report template links.</summary>
public sealed class ApplicationTypeGroupSeedUpdater : ModuleUpdater
{
    public ApplicationTypeGroupSeedUpdater(IObjectSpace objectSpace, Version currentDBVersion)
        : base(objectSpace, currentDBVersion)
    {
    }

    public override void UpdateDatabaseAfterUpdateSchema()
    {
        base.UpdateDatabaseAfterUpdateSchema();
        ApplicationTypeGroupSeed.EnsureRegistrationGroup(ObjectSpace);
        ObjectSpace.CommitChanges();
    }
}
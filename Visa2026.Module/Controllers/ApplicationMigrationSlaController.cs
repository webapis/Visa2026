using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>Blocks delete of <see cref="ApplicationMigrationSlaProfile"/> when referenced by <see cref="ApplicationType"/>.</summary>
public sealed class ApplicationMigrationSlaProfileDeleteController : ViewController
{
    public ApplicationMigrationSlaProfileDeleteController()
    {
        TargetObjectType = typeof(ApplicationMigrationSlaProfile);
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectDeleting += ObjectSpace_ObjectDeleting;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectDeleting -= ObjectSpace_ObjectDeleting;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectDeleting(object sender, ObjectsManipulatingEventArgs e)
    {
        foreach (var profile in e.Objects.OfType<ApplicationMigrationSlaProfile>())
        {
            var referenced = ObjectSpace.GetObjectsQuery<ApplicationType>()
                .Any(t => t.MigrationSlaProfileId == profile.ID);
            if (!referenced)
                continue;

            throw new UserFriendlyException(VisaUiMessages.Get("ApplicationMigrationSlaProfile.DeleteBlocked"));
        }
    }
}

/// <summary>Warns when <see cref="ApplicationType.MigrationSlaProfile"/> is missing (non-blocking).</summary>
public sealed class ApplicationTypeMigrationSlaWarningController : ObjectViewController<DetailView, ApplicationType>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.Committing += ObjectSpace_Committing;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committing -= ObjectSpace_Committing;
        base.OnDeactivated();
    }

    private void ObjectSpace_Committing(object sender, CancelEventArgs e)
    {
        foreach (var applicationType in ObjectSpace.GetObjectsToSave(false).OfType<ApplicationType>())
        {
            if (applicationType.MigrationSlaProfile != null)
                continue;

            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationType.MigrationSlaProfileMissingWarning"),
                InformationType.Warning,
                6000,
                InformationPosition.Top);
        }
    }
}

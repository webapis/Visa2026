using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Snapshots ministry legs when <see cref="BusinessObjects.Application.ProjectContract"/> changes; warns after progress exists.
/// </summary>
public sealed class ApplicationProjectContractMinistryController : ObjectViewController<DetailView, BusinessObjects.Application>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        base.OnDeactivated();
    }

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object is not BusinessObjects.Application application
            || e.PropertyName != nameof(BusinessObjects.Application.ProjectContract))
            return;

        var previousContract = e.OldValue as ProjectContract;
        var newContract = e.NewValue as ProjectContract;
        if (ReferenceEquals(previousContract, newContract))
            return;

        if (ApplicationProgressProfileResolver.HasProgressBeyondOfficePreparation(application, ObjectSpace))
        {
            application.ProjectContract = previousContract;
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("Application.ProjectContractLockedAfterProgress"),
                InformationType.Warning,
                8000,
                InformationPosition.Top);
            return;
        }

        ProjectContractMinistryHelper.ApplySnapshot(ObjectSpace, application, newContract);

        Application.ShowViewStrategy.ShowMessage(
            VisaUiMessages.Get("Application.ProjectContractChangedAfterProgress"),
            InformationType.Warning,
            8000,
            InformationPosition.Top);

        if (ApplicationProgressProfileResolver.WouldMinistryDepthChange(application, previousContract, newContract))
        {
            var previousLabel = ApplicationProgressProfileResolver.FormatMinistryLegCountLabel(
                ProjectContractMinistryHelper.GetLegCount(previousContract));
            var newLabel = ApplicationProgressProfileResolver.FormatMinistryLegCountLabel(
                ProjectContractMinistryHelper.GetLegCount(newContract));
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Format(
                    "Application.ProjectContractMinistryDepthChanged",
                    previousLabel,
                    newLabel),
                InformationType.Warning,
                8000,
                InformationPosition.Top);
        }
    }
}

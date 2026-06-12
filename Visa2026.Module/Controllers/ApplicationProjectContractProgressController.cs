using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Warns when <see cref="Application.ProjectContract"/> changes after progress history exists.
/// </summary>
public sealed class ApplicationProjectContractProgressController : ObjectViewController<DetailView, BusinessObjects.Application>
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

        if (!ApplicationProgressProfileResolver.HasAnyProgressHistory(application, ObjectSpace))
            return;

        var previousContract = e.OldValue as ProjectContract;
        var newContract = e.NewValue as ProjectContract;
        if (ReferenceEquals(previousContract, newContract))
            return;

        Application.ShowViewStrategy.ShowMessage(
            VisaUiMessages.Get("Application.ProjectContractChangedAfterProgress"),
            InformationType.Warning,
            8000,
            InformationPosition.Top);

        if (ApplicationProgressProfileResolver.WouldMinistryDepthChange(application, previousContract, newContract))
        {
            var previousDepth = ResolveDepthLabel(application, previousContract);
            var newDepth = ResolveDepthLabel(application, newContract);
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Format(
                    "Application.ProjectContractMinistryDepthChanged",
                    previousDepth,
                    newDepth),
                InformationType.Warning,
                8000,
                InformationPosition.Top);
        }
    }

    private static string ResolveDepthLabel(BusinessObjects.Application application, ProjectContract? contract)
    {
        var depth = contract != null
            ? ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                ApplicationProgressRouteKind.ViaMinistries,
                contract.MinistryReviewDepth)
            : ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                ApplicationProgressRouteKind.ViaMinistries,
                application.ApplicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);

        return ApplicationProgressProfileResolver.FormatMinistryReviewDepthLabel(depth);
    }
}

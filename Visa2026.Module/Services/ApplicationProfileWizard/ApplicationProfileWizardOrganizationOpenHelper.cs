using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationProfileWizard;

/// <summary>
/// Opens the live Configuration singleton DetailView (not a copy on the Application Profile).
/// </summary>
public static class ApplicationProfileWizardOrganizationOpenHelper
{
    public enum Kind
    {
        Company,
        Signatory,
        Representative
    }

    public static bool TryOpen(XafApplication application, Kind kind, Action? onClosed = null)
    {
        if (application == null)
            return false;

        var type = kind switch
        {
            Kind.Signatory => typeof(AuthorizedSignatory),
            Kind.Representative => typeof(AuthorizedRepresentative),
            _ => typeof(CompanyProfile)
        };

        var objectSpace = application.CreateObjectSpace(type);
        var current = kind switch
        {
            Kind.Signatory => (object)AuthorizedSignatory.GetOrCreateInstance(objectSpace),
            Kind.Representative => AuthorizedRepresentative.GetOrCreateInstance(objectSpace),
            _ => CompanyProfile.GetOrCreateInstance(objectSpace)
        };

        var detailView = application.CreateDetailView(objectSpace, current);
        detailView.ViewEditMode = ViewEditMode.Edit;

        if (onClosed != null)
        {
            EventHandler? committed = null;
            committed = (_, _) =>
            {
                objectSpace.Committed -= committed;
                onClosed();
            };
            objectSpace.Committed += committed;
            detailView.Closed += (_, _) => onClosed();
        }

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(application.MainWindow, null));
        return true;
    }
}
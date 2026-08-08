using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Workspace profile-strip actions: Configure wizard / New Application from the linked profile.
/// </summary>
public static class ApplicationWorkspaceProfileRailHelper
{
    public static bool TryCreateNewApplicationFromProfile(
        XafApplication application,
        Guid applicationProfileId,
        Guid contextApplicationId,
        Frame? sourceFrame,
        out string? errorMessage)
    {
        errorMessage = null;

        if (application == null || applicationProfileId == Guid.Empty)
        {
            errorMessage = "Select an Application Profile first.";
            return false;
        }

        ApplicationProgressRouteKind? route = null;
        if (contextApplicationId != Guid.Empty)
        {
            using var contextSpace = application.CreateObjectSpace(typeof(Application));
            var contextApp = contextSpace.GetObjectByKey<Application>(contextApplicationId);
            route = contextApp?.CreationProgressRoute
                ?? contextApp?.ApplicationProfile?.ProgressRoute
                ?? contextApp?.ApplicationType?.ApplicationProgressRoute;
        }

        ApplicationProfilePickerContextGate.Set(
            application,
            new ApplicationProfilePickerOpenContext { CreationProgressRoute = route },
            sourceFrame ?? application.MainWindow);

        if (ApplicationProfilePickerCompletionHelper.TryCreateApplication(
                application,
                applicationProfileId,
                out errorMessage))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            application.ShowViewStrategy.ShowMessage(errorMessage, InformationType.Warning);
        }

        return false;
    }

    public static bool TryOpenProfileConfiguration(
        XafApplication application,
        Guid applicationProfileId,
        Frame? sourceFrame)
    {
        if (application == null || applicationProfileId == Guid.Empty)
            return false;

        var wizardView = ApplicationProfileWizardOpenHelper.CreateWizardView(application, applicationProfileId);
        if (wizardView == null)
        {
            application.ShowViewStrategy.ShowMessage(
                "Application Profile not found or wizard could not be opened.",
                InformationType.Warning);
            return false;
        }

        var frame = sourceFrame ?? application.MainWindow;
        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(wizardView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(frame, null));

        return true;
    }
}

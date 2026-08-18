using System;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationProfileWizard;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Workspace profile-strip actions: Configure wizard / New ApplicationProfileInstance from the linked profile.
/// </summary>
public static class ApplicationWorkspaceProfileRailHelper
{
    public static bool TryCreateNewApplicationFromProfile(
        XafApplication application,
        Guid applicationProfileId,
        Guid contextApplicationProfileInstanceId,
        Frame? sourceFrame,
        out string? errorMessage)
    {
        errorMessage = null;

        if (application == null || applicationProfileId == Guid.Empty)
        {
            errorMessage = "Select an Application Profile first.";
            return false;
        }

        ApplicationProfileInstanceProgressRouteKind? route = null;
        if (contextApplicationProfileInstanceId != Guid.Empty)
        {
            using var contextSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance));
            var contextApp = contextSpace.GetObjectByKey<ApplicationProfileInstance>(contextApplicationProfileInstanceId);
            route = contextApp?.CreationProgressRoute
                ?? contextApp?.ApplicationProfile?.ProgressRoute
                ?? contextApp?.ApplicationType?.ApplicationProfileInstanceProgressRoute;
        }

        ApplicationProfilePickerContextGate.Set(
            application,
            new ApplicationProfilePickerOpenContext { CreationProgressRoute = route },
            sourceFrame ?? application.MainWindow);

        using var profileSpace = application.CreateObjectSpace(typeof(ApplicationProfile));
        var versions = ApplicationProfileApprovalLegVersionHelper.GetOrderedVersions(
            profileSpace.GetObjectByKey<ApplicationProfile>(applicationProfileId));
        var versionId = versions.Count == 1 ? versions[0].ID : (Guid?)null;
        var needsPicker = versions.Count > 1;
        if (needsPicker)
        {
            var pickerView = ApplicationProfilePickerOpenHelper.CreatePickerView(
                application,
                new ApplicationProfilePickerOpenContext { CreationProgressRoute = route },
                sourceFrame ?? application.MainWindow);
            if (pickerView == null)
            {
                errorMessage = "Choose an approval-leg version from New application.";
                return false;
            }

            var frame = sourceFrame ?? application.MainWindow;
            application.ShowViewStrategy.ShowView(
                new ShowViewParameters(pickerView) { TargetWindow = TargetWindow.Current },
                new ShowViewSource(frame, null));
            return true;
        }

        if (ApplicationProfilePickerCompletionHelper.TryCreateApplication(
                application,
                applicationProfileId,
                versionId,
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

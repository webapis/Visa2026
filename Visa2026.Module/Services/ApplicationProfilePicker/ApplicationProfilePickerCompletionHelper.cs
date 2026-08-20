using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

public static class ApplicationProfilePickerCompletionHelper
{
    public static bool TryCreateApplication(
        XafApplication application,
        Guid profileId,
        out string? errorMessage) =>
        TryCreateApplication(application, profileId, approvalLegVersionId: null, out errorMessage);

    public static bool TryCreateApplication(
        XafApplication application,
        Guid profileId,
        Guid? approvalLegVersionId,
        out string? errorMessage)
    {
        errorMessage = null;

        if (application == null || profileId == Guid.Empty)
        {
            errorMessage = "Select an Application Profile first.";
            return false;
        }

        var context = ApplicationProfilePickerContextGate.Get(application);
        if (context?.SeedPersonId is Guid seedPersonId && seedPersonId != Guid.Empty)
        {
            errorMessage = "Select people for this ApplicationProfileInstance first.";
            return false;
        }

        return TryCreateApplicationCore(application, profileId, null, approvalLegVersionId, out errorMessage, out _);
    }

    public static bool TryCreateApplicationFromPersonStart(
        XafApplication application,
        Guid profileId,
        IReadOnlyList<Guid> personIds,
        out string? errorMessage,
        out string? successMessage) =>
        TryCreateApplicationFromPersonStart(
            application, profileId, personIds, approvalLegVersionId: null, out errorMessage, out successMessage);

    public static bool TryCreateApplicationFromPersonStart(
        XafApplication application,
        Guid profileId,
        IReadOnlyList<Guid> personIds,
        Guid? approvalLegVersionId,
        out string? errorMessage,
        out string? successMessage)
    {
        errorMessage = null;
        successMessage = null;

        if (application == null || profileId == Guid.Empty)
        {
            errorMessage = "Select an Application Profile first.";
            return false;
        }

        if (personIds == null || personIds.Count == 0)
        {
            errorMessage = "Select at least one person.";
            return false;
        }

        var context = ApplicationProfilePickerContextGate.Get(application);
        if (context?.SeedPersonId is not Guid seedPersonId || seedPersonId == Guid.Empty)
        {
            errorMessage = "Person start context is missing.";
            return false;
        }

        using var validateSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var profile = validateSpace.GetObjectByKey<ApplicationProfile>(profileId);
        var seedPerson = validateSpace.GetObjectByKey<Person>(seedPersonId);
        if (profile == null || seedPerson == null)
        {
            errorMessage = "Application Profile or seed person not found.";
            return false;
        }

        var selectedPeople = personIds
            .Distinct()
            .Select(id => validateSpace.GetObjectByKey<Person>(id))
            .Where(p => p != null)
            .Cast<Person>()
            .ToList();

        var validation = ApplicationStartFromPersonHelper.Validate(
            validateSpace,
            profile,
            seedPerson,
            selectedPeople);
        if (validation.IsBlocked)
        {
            errorMessage = string.Join(" ", validation.Errors);
            return false;
        }

        if (!TryCreateApplicationCore(application, profileId, selectedPeople, approvalLegVersionId, out errorMessage, out var appNumber))
            return false;

        var warningText = validation.Warnings.Count > 0
            ? " " + string.Join(" ", validation.Warnings)
            : string.Empty;

        if (context.StayOnSourceAfterCreate)
        {
            var dossierView = PersonDossierOpenHelper.CreateDossierView(application, seedPersonId);
            if (dossierView != null)
            {
                ShowViewInCurrentWindow(application, dossierView);
            }

            successMessage = $"ApplicationProfileInstance {appNumber} created.{warningText}";
            application.ShowViewStrategy.ShowMessage(
                successMessage,
                InformationType.Success,
                5000);
            return true;
        }

        successMessage = validation.Warnings.Count > 0 ? warningText.Trim() : null;
        return true;
    }

    private static bool TryCreateApplicationCore(
        XafApplication application,
        Guid profileId,
        IReadOnlyList<Person>? peopleToLink,
        Guid? approvalLegVersionId,
        out string? errorMessage,
        out string? applicationNumber)
    {
        errorMessage = null;
        applicationNumber = null;

        var context = ApplicationProfilePickerContextGate.Get(application);

        var objectSpace = application.CreateObjectSpace(typeof(ApplicationProfileInstance));
        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId);
        if (profile == null)
        {
            errorMessage = "Application Profile not found.";
            return false;
        }

        if (!ApplicationProfileApplicabilityHelper.IsProfileSelectable(
                profile,
                null,
                context?.CreationProgressRoute))
        {
            errorMessage = "This ApplicationProfileInstance Profile is not available for the current route or criteria.";
            return false;
        }

        if (!ApplicationProfileApprovalLegVersionHelper.TryResolveSharedProfileForCreate(
                profile,
                approvalLegVersionId,
                objectSpace,
                out var sharedProfile,
                out errorMessage))
        {
            return false;
        }

        // Legacy nested versions if the shared catalog is empty
        if (sharedProfile == null
            && profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
            && ApplicationProfileApprovalLegVersionHelper.GetOrderedVersions(profile).Count > 0)
        {
            if (!ApplicationProfileApprovalLegVersionHelper.TryResolveVersionForCreate(
                    profile,
                    approvalLegVersionId,
                    out var nestedVersion,
                    out errorMessage))
            {
                return false;
            }

            var nestedApp = objectSpace.CreateObject<ApplicationProfileInstance>();
            ApplicationProfilePickerApplyHelper.ApplyProfileToNewApplication(
                objectSpace,
                nestedApp,
                profile,
                context?.CreationProgressRoute);
            ApplicationProfileApprovalLegVersionHelper.ApplySnapshot(objectSpace, nestedApp, nestedVersion);
            if (peopleToLink != null && peopleToLink.Count > 0)
                ApplicationStartFromPersonHelper.LinkPeople(objectSpace, nestedApp, peopleToLink);
            objectSpace.CommitChanges();
            applicationNumber = nestedApp.FullApplicationNumber ?? nestedApp.ApplicationNumber ?? nestedApp.ID.ToString();
            if (context?.StayOnSourceAfterCreate == true)
                return true;
            var nestedCommitted = objectSpace.GetObject(nestedApp);
            var nestedWorkspace = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(application, nestedCommitted.ID);
            if (nestedWorkspace == null)
            {
                errorMessage = "ApplicationProfileInstance was created but the workspace could not be opened.";
                return false;
            }
            ShowViewInCurrentWindow(application, nestedWorkspace);
            return true;
        }

        var app = objectSpace.CreateObject<ApplicationProfileInstance>();
        ApplicationProfilePickerApplyHelper.ApplyProfileToNewApplication(
            objectSpace,
            app,
            profile,
            context?.CreationProgressRoute);

        ApplicationProfileApprovalLegVersionHelper.ApplySharedSnapshot(objectSpace, app, sharedProfile);

        if (peopleToLink != null && peopleToLink.Count > 0)
            ApplicationStartFromPersonHelper.LinkPeople(objectSpace, app, peopleToLink);

        objectSpace.CommitChanges();
        applicationNumber = app.FullApplicationNumber ?? app.ApplicationNumber ?? app.ID.ToString();

        if (context?.StayOnSourceAfterCreate == true)
            return true;

        var committedApp = objectSpace.GetObject(app);
        var workspaceView = ApplicationWorkspaceOpenHelper.CreateWorkspaceView(application, committedApp.ID);
        if (workspaceView == null)
        {
            errorMessage = "ApplicationProfileInstance was created but the workspace could not be opened.";
            return false;
        }

        ShowViewInCurrentWindow(application, workspaceView);

        return true;
    }

    private static void ShowViewInCurrentWindow(XafApplication application, View view)
    {
        var sourceFrame = ApplicationProfilePickerContextGate.GetSourceFrame(application)
            ?? application.MainWindow;

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(view) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(sourceFrame, null));
    }
}

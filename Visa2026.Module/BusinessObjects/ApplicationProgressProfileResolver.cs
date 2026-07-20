using System;

using System.Linq;

using DevExpress.ExpressApp;

using Visa2026.Module.Localization;



namespace Visa2026.Module.BusinessObjects;



/// <summary>

/// Resolves effective progress route settings for an <see cref="Application"/>

/// (type defaults, <see cref="ApprovalLegProfile"/> legs, snapshots).

/// </summary>

public static class ApplicationProgressProfileResolver

{

    /// <summary>

    /// Header members locked after approval leaves office preparation. Workflow fields (visa, travel,

    /// locations, child collections) and optional detail fields remain editable for later process steps.

    /// </summary>

    public const string LockedApplicationHeaderTargetItems =

        "IsManualEntry;ApplicationNumber;AppNumberPrefix;FullApplicationNumber;ApplicationDate;ApplicationTypeQuickCode;ApprovalLegProfile;ProjectContract";

    /// <summary>
    /// Detail members read-only when <see cref="Application.IsWorkflowTerminal"/> is true.
    /// Excludes <see cref="Application.ProgressHistory"/> so officers may delete the last step.
    /// </summary>
    public const string TerminalLockedApplicationDetailTargetItems =
        "IsManualEntry;ApplicationNumber;AppNumberPrefix;FullApplicationNumber;ApplicationDate;ApplicationTypeQuickCode;ApplicationType;ApplicationReason;ApprovalLegProfile;ProjectContract;MigrationService;FromCity;ToCity;BusinessTripStartDate;BusinessTripEndDate;BusinessTripPurpose;VisaPeriod;VisaType;VisaCategory;MovementPermitLocation;BorderZoneLocation;Urgency;IsForFamily;OrganizationType;ApplicationItems;Invitations;Rejections;WorkPermits";



    public static bool RequiresProjectContract(Application? application)

    {

        if (application?.ApplicationType?.ShowProjectContract != true)

            return false;



        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);

        return route == ApplicationProgressRouteKind.ViaMinistries;

    }



    public static bool RequiresApprovalLegProfile(Application? application)

    {

        if (application?.ApplicationType?.ShowApprovalLegProfile != true)

            return false;



        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);

        return route == ApplicationProgressRouteKind.ViaMinistries;

    }



    public static int GetMinistryLegCount(Application? application)

    {

        if (application == null)

            return 1;



        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);

        if (!route.HasValue || route.Value == ApplicationProgressRouteKind.DirectToMigrationService)

            return 0;



        var snapshotCount = application.ApprovalLegSnapshots?

            .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;

        if (snapshotCount > 0)

            return snapshotCount;



        if (application.ApplicationType?.ShowApprovalLegProfile == true

            && application.ApprovalLegProfile != null)

        {

            return ApprovalLegProfileMinistryHelper.GetLegCount(application.ApprovalLegProfile);

        }



        return MapLegacyDepthToLegCount(

            application.ApplicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);

    }



    public static MinistryReviewDepth GetMinistryReviewDepth(Application? application)

    {

        var legCount = GetMinistryLegCount(application);

        return MapLegCountToLegacyDepth(legCount);

    }



    public static MinistryReviewDepth GetMinistryReviewDepth(ApplicationType? applicationType) =>

        applicationType == null

            ? MinistryReviewDepth.FirstMinistryOnly

            : ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(

                applicationType.ApplicationProgressRoute,

                applicationType.MinistryReviewDepth);



    public static bool HasAnyProgressHistory(Application? application, IObjectSpace? objectSpace = null) =>

        ApplicationProgressHelper.GetLatest(application?.ProgressHistory, objectSpace) != null;



    public static bool HasProgressBeyondOfficePreparation(Application? application, IObjectSpace? objectSpace = null)

    {

        if (application?.ProgressHistory == null)

            return false;



        return application.ProgressHistory.Any(p =>

            (objectSpace == null || !objectSpace.IsObjectToDelete(p))

            && !IsOfficePreparationStep(p));

    }



    public static bool IsApplicationLockedAfterOfficePreparation(

        Application? application,

        IObjectSpace? objectSpace = null) =>

        HasProgressBeyondOfficePreparation(application, objectSpace);

    public static bool IsWorkflowTerminal(Application? application) =>
        IsProcessTerminalStateCode(ApplicationProgressPrimaryStateCodeResolver.Resolve(application));

    public static bool IsProcessTerminalStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        return string.Equals(trimmed, ApplicationProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ApplicationProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase);
    }



    public static bool IsProjectContractLocked(Application? application, IObjectSpace? objectSpace = null)

    {

        if (application?.ApplicationType?.ShowProjectContract != true)

            return false;



        return IsApplicationLockedAfterOfficePreparation(application, objectSpace);

    }



    public static bool IsApprovalLegProfileLocked(Application? application, IObjectSpace? objectSpace = null)

    {

        if (application?.ApplicationType?.ShowApprovalLegProfile != true)

            return false;



        return IsApplicationLockedAfterOfficePreparation(application, objectSpace);

    }



    public static bool TryValidateApplicationUnchangedAfterProgress(

        Application? application,

        IObjectSpace objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        if (application == null || objectSpace.IsNewObject(application))

            return true;



        if (!HasProgressBeyondOfficePreparation(application, objectSpace))

            return true;



        var original = objectSpace.GetObjectByKey<Application>(application.ID);

        if (original == null)

            return true;



        if (ApplicationLockedHeaderScalarsDiffer(original, application))

        {

            errorMessage = VisaUiMessages.Get("Application.FieldsLockedAfterProgress");

            return false;

        }



        return true;

    }

    public static bool TryValidateApplicationEditableWhenWorkflowTerminal(

        IObjectSpace objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        foreach (var application in objectSpace.GetObjectsToSave(false).OfType<Application>())

        {

            if (objectSpace.IsNewObject(application))

                continue;

            var original = objectSpace.GetObjectByKey<Application>(application.ID);

            if (original == null || !IsWorkflowTerminal(original))

                continue;

            errorMessage = VisaUiMessages.Get("Application.FieldsLockedWhenWorkflowTerminal");

            return false;

        }

        foreach (var child in objectSpace.GetObjectsToSave(false))

        {

            if (child is Application or ApplicationProgress)

                continue;

            var parent = TryGetParentApplication(child);

            if (parent == null || objectSpace.IsNewObject(parent))

                continue;

            var originalParent = objectSpace.GetObjectByKey<Application>(parent.ID);

            if (originalParent == null || !IsWorkflowTerminal(originalParent))

                continue;

            errorMessage = VisaUiMessages.Get("Application.FieldsLockedWhenWorkflowTerminal");

            return false;

        }

        return true;

    }

    private static Application? TryGetParentApplication(object entity) =>

        entity switch

        {

            ApplicationItem item => item.Application,

            Invitation invitation => invitation.Application,

            Rejection rejection => rejection.Application,

            WorkPermit workPermit => workPermit.Application,

            _ => null

        };



    public static bool TryValidateProjectContractUnchangedAfterProgress(

        Application? application,

        IObjectSpace objectSpace,

        out string? errorMessage) =>

        TryValidateApplicationUnchangedAfterProgress(application, objectSpace, out errorMessage);



    public static bool TryValidateProjectContractOnApplication(

        Application? application,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        if (application == null)

            return true;



        if (!TryValidateApprovalLegProfileOnApplication(application, objectSpace, out errorMessage))

            return false;



        if (!RequiresProjectContract(application))

            return true;



        if (application.ProjectContract != null)

            return true;



        if (!HasProgressBeyondOfficePreparation(application, objectSpace))

            return true;



        errorMessage = VisaUiMessages.Get("ApplicationProgress.ProjectContractRequired");

        return false;

    }



    public static bool TryValidateApprovalLegProfileOnApplication(

        Application? application,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        if (application == null || !RequiresApprovalLegProfile(application))

            return true;



        if (application.ApprovalLegProfile != null)

        {

            if (ApprovalLegProfileMinistryHelper.HasConfiguredLegs(application.ApprovalLegProfile))

                return true;



            if (!HasProgressBeyondOfficePreparation(application, objectSpace))

                return true;



            errorMessage = VisaUiMessages.Get("Application.ApprovalLegProfileLegsRequired");

            return false;

        }



        if (!HasProgressBeyondOfficePreparation(application, objectSpace))

            return true;



        errorMessage = VisaUiMessages.Get("ApplicationProgress.ApprovalLegProfileRequired");

        return false;

    }



    public static bool TryValidateProjectContractForProgress(

        ApplicationProgress progress,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        var application = progress.Application;

        if (application == null)

            return true;



        if (!TryValidateApprovalLegProfileForProgress(progress, objectSpace, out errorMessage))

            return false;



        if (!RequiresProjectContract(application))

            return true;



        if (application.ProjectContract != null)

            return true;



        if (IsPermittedWithoutProjectContract(progress, objectSpace))

            return true;



        errorMessage = VisaUiMessages.Get("ApplicationProgress.ProjectContractRequired");

        return false;

    }



    public static bool TryValidateApprovalLegProfileForProgress(

        ApplicationProgress progress,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        var application = progress.Application;

        if (application == null || !RequiresApprovalLegProfile(application))

            return true;



        if (application.ApprovalLegProfile != null

            && TryValidateApprovalLegProfileOnApplication(application, objectSpace, out errorMessage))

            return errorMessage == null;



        if (application.ApprovalLegProfile != null)

            return false;



        if (IsPermittedWithoutApprovalLegProfile(progress, objectSpace))

            return true;



        errorMessage = VisaUiMessages.Get("ApplicationProgress.ApprovalLegProfileRequired");

        return false;

    }



    public static bool WouldMinistryDepthChange(

        Application application,

        ProjectContract? previousContract,

        ProjectContract? newContract) =>

        false;



    public static bool WouldMinistryDepthChange(

        Application application,

        ApprovalLegProfile? previousProfile,

        ApprovalLegProfile? newProfile)

    {

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);

        if (route != ApplicationProgressRouteKind.ViaMinistries)

            return false;



        if (application.ApplicationType?.ShowApprovalLegProfile != true)

            return false;



        return ResolveLegCountForProfile(application, previousProfile)

            != ResolveLegCountForProfile(application, newProfile);

    }



    public static string FormatMinistryReviewDepthLabel(MinistryReviewDepth depth) =>

        depth == MinistryReviewDepth.FirstAndSecondMinistry

            ? VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.Two")

            : depth == MinistryReviewDepth.FirstMinistryOnly

                ? VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.One")

                : VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.None");



    public static string FormatMinistryLegCountLabel(int legCount) =>

        legCount switch

        {

            0 => VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.None"),

            1 => VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.One"),

            2 => VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.Two"),

            _ => VisaUiMessages.Format("ApplicationProgressProfile.MinistryDepth.Many", legCount)

        };



    private static int ResolveLegCountForProfile(Application application, ApprovalLegProfile? profile)

    {

        if (profile == null)

            return GetMinistryLegCount(application);



        if (ReferenceEquals(application.ApprovalLegProfile, profile))

        {

            var snapshotCount = application.ApprovalLegSnapshots?

                .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;

            if (snapshotCount > 0)

                return snapshotCount;

        }



        return ApprovalLegProfileMinistryHelper.GetLegCount(profile);

    }



    private static int MapLegacyDepthToLegCount(MinistryReviewDepth depth) =>

        depth == MinistryReviewDepth.FirstAndSecondMinistry ? 2 : 1;



    private static MinistryReviewDepth MapLegCountToLegacyDepth(int legCount) =>

        legCount switch

        {

            <= 0 => MinistryReviewDepth.None,

            1 => MinistryReviewDepth.FirstMinistryOnly,

            _ => MinistryReviewDepth.FirstAndSecondMinistry

        };



    private static bool IsPermittedWithoutProjectContract(ApplicationProgress progress, IObjectSpace? objectSpace)

    {

        if (!IsOfficePreparationStep(progress))

            return false;



        var application = progress.Application;

        if (application?.ProgressHistory == null)

            return true;



        var otherRows = application.ProgressHistory

            .Where(p => p != progress && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))

            .ToList();



        return otherRows.Count == 0;

    }



    private static bool IsPermittedWithoutApprovalLegProfile(ApplicationProgress progress, IObjectSpace? objectSpace)

    {

        if (!IsOfficePreparationStep(progress))

            return false;



        var application = progress.Application;

        if (application?.ProgressHistory == null)

            return true;



        var otherRows = application.ProgressHistory

            .Where(p => p != progress && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))

            .ToList();



        return otherRows.Count == 0;

    }



        private static bool IsOfficePreparationStep(ApplicationProgress progress) =>
        progress.State != null
        && string.Equals(progress.State.Code, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase);



    private static bool ApplicationLockedHeaderScalarsDiffer(Application original, Application current) =>

        original.IsManualEntry != current.IsManualEntry

        || !string.Equals(original.ApplicationNumber, current.ApplicationNumber, StringComparison.Ordinal)

        || !string.Equals(original.AppNumberPrefix, current.AppNumberPrefix, StringComparison.Ordinal)

        || !string.Equals(original.FullApplicationNumber, current.FullApplicationNumber, StringComparison.Ordinal)

        || original.ApplicationDate != current.ApplicationDate

        || original.ApplicationType?.ID != current.ApplicationType?.ID

        || original.ApprovalLegProfile?.ID != current.ApprovalLegProfile?.ID

        || original.ProjectContract?.ID != current.ProjectContract?.ID;

}



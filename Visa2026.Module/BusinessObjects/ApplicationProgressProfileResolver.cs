using System;

using System.Linq;

using DevExpress.ExpressApp;

using Visa2026.Module.Localization;



namespace Visa2026.Module.BusinessObjects;



/// <summary>

/// Resolves effective progress route settings for an <see cref="Application"/>

/// (type defaults, <see cref="ApprovalLegProfile"/> legs, snapshots).

/// </summary>

public static class ApplicationProfileInstanceProgressProfileResolver

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
        "IsManualEntry;ApplicationNumber;AppNumberPrefix;FullApplicationNumber;ApplicationDate;ApplicationTypeQuickCode;ApplicationType;ApplicationReason;ApprovalLegProfile;ProjectContract;MigrationService;FromCity;ToCity;BusinessTripStartDate;BusinessTripEndDate;BusinessTripPurpose;VisaPeriod;VisaType;VisaCategory;MovementPermitLocation;BorderZoneLocation;Urgency;IsForFamily;ApplicationItems;Invitations;Rejections;WorkPermits";



    public static bool RequiresProjectContract(ApplicationProfileInstance? application)

    {

        if (!ApplicationProfileConfigurationResolver.ShowProjectContract(application))

            return false;



        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);

        return route == ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

    }



    public static bool RequiresApprovalLegProfile(ApplicationProfileInstance? application)

    {

        if (!ApplicationProfileConfigurationResolver.ShowApprovalLegProfile(application))

            return false;



        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);

        return route == ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

    }



    public static int GetMinistryLegCount(ApplicationProfileInstance? application)

    {

        if (application == null)

            return 1;



        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);

        if (!route.HasValue || route.Value == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)

            return 0;



        var snapshotCount = application.ApprovalLegSnapshots?

            .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;

        if (snapshotCount > 0)

            return snapshotCount;



        var embeddedLegCount = ApplicationProfileConfigurationResolver.GetEmbeddedProfileMinistryLegCount(application);

        if (embeddedLegCount > 0)

            return embeddedLegCount;



        if (ApplicationProfileConfigurationResolver.ShowApprovalLegProfile(application)

            && application.ApprovalLegProfile != null)

        {

            return ApprovalLegProfileMinistryHelper.GetLegCount(application.ApprovalLegProfile);

        }



        return MapLegacyDepthToLegCount(

            application.ApplicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);

    }



    public static MinistryReviewDepth GetMinistryReviewDepth(ApplicationProfileInstance? application)

    {

        var legCount = GetMinistryLegCount(application);

        return MapLegCountToLegacyDepth(legCount);

    }



    public static MinistryReviewDepth GetMinistryReviewDepth(ApplicationType? applicationType) =>

        applicationType == null

            ? MinistryReviewDepth.FirstMinistryOnly

            : ApplicationProfileInstanceProgressRouteHelper.NormalizeMinistryReviewDepth(

                applicationType.ApplicationProfileInstanceProgressRoute,

                applicationType.MinistryReviewDepth);



    public static bool HasAnyProgressHistory(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null) =>

        ApplicationProfileInstanceProgressHelper.GetLatest(application?.ProgressHistory, objectSpace) != null;



    public static bool HasProgressBeyondOfficePreparation(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null)

    {

        if (application?.ProgressHistory == null)

            return false;



        return application.ProgressHistory.Any(p =>

            (objectSpace == null || !objectSpace.IsObjectToDelete(p))

            && !IsOfficePreparationStep(p));

    }



    public static bool IsApplicationLockedAfterOfficePreparation(

        ApplicationProfileInstance? application,

        IObjectSpace? objectSpace = null) =>

        HasProgressBeyondOfficePreparation(application, objectSpace);

    public static bool IsWorkflowTerminal(ApplicationProfileInstance? application) =>
        IsProcessTerminalStateCode(ApplicationProfileInstanceProgressPrimaryStateCodeResolver.Resolve(application));

    public static bool IsProcessTerminalStateCode(string? stateCode)
    {
        if (string.IsNullOrWhiteSpace(stateCode))
            return false;

        var trimmed = stateCode.Trim();
        return string.Equals(trimmed, ApplicationProfileInstanceProgressStateCodes.ProcessCancelled, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ApplicationProfileInstanceProgressStateCodes.ProcessRejected, StringComparison.OrdinalIgnoreCase)
            || string.Equals(trimmed, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase);
    }



    public static bool IsProjectContractLocked(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null)

    {

        if (!ApplicationProfileConfigurationResolver.ShowProjectContract(application))

            return false;



        return IsApplicationLockedAfterOfficePreparation(application, objectSpace);

    }



    public static bool IsApprovalLegProfileLocked(ApplicationProfileInstance? application, IObjectSpace? objectSpace = null)

    {

        if (!ApplicationProfileConfigurationResolver.ShowApprovalLegProfile(application))

            return false;



        return IsApplicationLockedAfterOfficePreparation(application, objectSpace);

    }



    public static bool TryValidateApplicationUnchangedAfterProgress(

        ApplicationProfileInstance? application,

        IObjectSpace objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        if (application == null || objectSpace.IsNewObject(application))

            return true;



        if (!HasProgressBeyondOfficePreparation(application, objectSpace))

            return true;



        var original = objectSpace.GetObjectByKey<ApplicationProfileInstance>(application.ID);

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

        foreach (var application in objectSpace.GetObjectsToSave(false).OfType<ApplicationProfileInstance>())

        {

            if (objectSpace.IsNewObject(application))

                continue;

            var original = objectSpace.GetObjectByKey<ApplicationProfileInstance>(application.ID);

            if (original == null || !IsWorkflowTerminal(original))

                continue;

            errorMessage = VisaUiMessages.Get("Application.FieldsLockedWhenWorkflowTerminal");

            return false;

        }

        foreach (var child in objectSpace.GetObjectsToSave(false))

        {

            if (child is ApplicationProfileInstance or ApplicationProfileInstanceProgress)

                continue;

            var parent = TryGetParentApplication(child);

            if (parent == null || objectSpace.IsNewObject(parent))

                continue;

            var originalParent = objectSpace.GetObjectByKey<ApplicationProfileInstance>(parent.ID);

            if (originalParent == null || !IsWorkflowTerminal(originalParent))

                continue;

            errorMessage = VisaUiMessages.Get("Application.FieldsLockedWhenWorkflowTerminal");

            return false;

        }

        return true;

    }

    private static ApplicationProfileInstance? TryGetParentApplication(object entity) =>

        entity switch

        {

            ApplicationRosterMergeLine item => item.ApplicationProfileInstance,

            Invitation invitation => invitation.ApplicationProfileInstance,

            Rejection rejection => rejection.ApplicationProfileInstance,

            WorkPermit workPermit => workPermit.ApplicationProfileInstance,

            _ => null

        };



    public static bool TryValidateProjectContractUnchangedAfterProgress(

        ApplicationProfileInstance? application,

        IObjectSpace objectSpace,

        out string? errorMessage) =>

        TryValidateApplicationUnchangedAfterProgress(application, objectSpace, out errorMessage);



    public static bool TryValidateProjectContractOnApplication(

        ApplicationProfileInstance? application,

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



        errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.ProjectContractRequired");

        return false;

    }



    public static bool TryValidateApprovalLegProfileOnApplication(

        ApplicationProfileInstance? application,

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



        errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.ApprovalLegProfileRequired");

        return false;

    }



    public static bool TryValidateProjectContractForProgress(

        ApplicationProfileInstanceProgress progress,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        var application = progress.ApplicationProfileInstance;

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



        errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.ProjectContractRequired");

        return false;

    }



    public static bool TryValidateApprovalLegProfileForProgress(

        ApplicationProfileInstanceProgress progress,

        IObjectSpace? objectSpace,

        out string? errorMessage)

    {

        errorMessage = null;

        var application = progress.ApplicationProfileInstance;

        if (application == null || !RequiresApprovalLegProfile(application))

            return true;



        if (application.ApprovalLegProfile != null

            && TryValidateApprovalLegProfileOnApplication(application, objectSpace, out errorMessage))

            return errorMessage == null;



        if (application.ApprovalLegProfile != null)

            return false;



        if (IsPermittedWithoutApprovalLegProfile(progress, objectSpace))

            return true;



        errorMessage = VisaUiMessages.Get("ApplicationProfileInstanceProgress.ApprovalLegProfileRequired");

        return false;

    }



    public static bool WouldMinistryDepthChange(

        ApplicationProfileInstance application,

        ProjectContract? previousContract,

        ProjectContract? newContract) =>

        false;



    public static bool WouldMinistryDepthChange(

        ApplicationProfileInstance application,

        ApprovalLegProfile? previousProfile,

        ApprovalLegProfile? newProfile)

    {

        var route = ApplicationProfileInstanceProgressRouteHelper.GetTypePickerRouteFilter(application);

        if (route != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)

            return false;



        if (!ApplicationProfileConfigurationResolver.ShowApprovalLegProfile(application))

            return false;



        return ResolveLegCountForProfile(application, previousProfile)

            != ResolveLegCountForProfile(application, newProfile);

    }



    public static string FormatMinistryReviewDepthLabel(MinistryReviewDepth depth) =>

        depth == MinistryReviewDepth.FirstAndSecondMinistry

            ? VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.Two")

            : depth == MinistryReviewDepth.FirstMinistryOnly

                ? VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.One")

                : VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.None");



    public static string FormatMinistryLegCountLabel(int legCount) =>

        legCount switch

        {

            0 => VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.None"),

            1 => VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.One"),

            2 => VisaUiMessages.Get("ApplicationProfileInstanceProgressProfile.MinistryDepth.Two"),

            _ => VisaUiMessages.Format("ApplicationProfileInstanceProgressProfile.MinistryDepth.Many", legCount)

        };



    private static int ResolveLegCountForProfile(ApplicationProfileInstance application, ApprovalLegProfile? profile)

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



    private static bool IsPermittedWithoutProjectContract(ApplicationProfileInstanceProgress progress, IObjectSpace? objectSpace)

    {

        if (!IsOfficePreparationStep(progress))

            return false;



        var application = progress.ApplicationProfileInstance;

        if (application?.ProgressHistory == null)

            return true;



        var otherRows = application.ProgressHistory

            .Where(p => p != progress && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))

            .ToList();



        return otherRows.Count == 0;

    }



    private static bool IsPermittedWithoutApprovalLegProfile(ApplicationProfileInstanceProgress progress, IObjectSpace? objectSpace)

    {

        if (!IsOfficePreparationStep(progress))

            return false;



        var application = progress.ApplicationProfileInstance;

        if (application?.ProgressHistory == null)

            return true;



        var otherRows = application.ProgressHistory

            .Where(p => p != progress && (objectSpace == null || !objectSpace.IsObjectToDelete(p)))

            .ToList();



        return otherRows.Count == 0;

    }



        private static bool IsOfficePreparationStep(ApplicationProfileInstanceProgress progress) =>
        progress.State != null
        && string.Equals(progress.State.Code, ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase);



    private static bool ApplicationLockedHeaderScalarsDiffer(ApplicationProfileInstance original, ApplicationProfileInstance current) =>

        original.IsManualEntry != current.IsManualEntry

        || !string.Equals(original.ApplicationNumber, current.ApplicationNumber, StringComparison.Ordinal)

        || !string.Equals(original.AppNumberPrefix, current.AppNumberPrefix, StringComparison.Ordinal)

        || !string.Equals(original.FullApplicationNumber, current.FullApplicationNumber, StringComparison.Ordinal)

        || original.ApplicationDate != current.ApplicationDate

        || original.ApplicationType?.ID != current.ApplicationType?.ID

        || original.ApprovalLegProfile?.ID != current.ApprovalLegProfile?.ID

        || original.ProjectContract?.ID != current.ProjectContract?.ID;

}



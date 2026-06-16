using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves effective progress route settings for an <see cref="Application"/>
/// (type defaults, <see cref="ProjectContract"/> ministry legs, snapshots).
/// </summary>
public static class ApplicationProgressProfileResolver
{
    public static bool RequiresProjectContract(Application? application)
    {
        if (application?.ApplicationType?.ShowProjectContract != true)
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

        if (application.ApplicationType?.ShowProjectContract == true && application.ProjectContract != null)
            return ProjectContractMinistryHelper.GetLegCount(application.ProjectContract);

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

    /// <summary>
    /// True when <see cref="Application.ProjectContract"/> must not be edited
    /// (ministry or migration progress recorded after office preparation).
    /// </summary>
    public static bool IsProjectContractLocked(Application? application, IObjectSpace? objectSpace = null)
    {
        if (application?.ApplicationType?.ShowProjectContract != true)
            return false;

        return HasProgressBeyondOfficePreparation(application, objectSpace);
    }

    /// <summary>Blocks <see cref="Application.ProjectContract"/> FK changes once approval has left office preparation.</summary>
    public static bool TryValidateProjectContractUnchangedAfterProgress(
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

        var originalContractId = original.ProjectContract?.ID ?? Guid.Empty;
        var currentContractId = application.ProjectContract?.ID ?? Guid.Empty;
        if (originalContractId == currentContractId)
            return true;

        errorMessage = VisaUiMessages.Get("Application.ProjectContractLockedAfterProgress");
        return false;
    }

    public static bool TryValidateProjectContractOnApplication(
        Application? application,
        IObjectSpace? objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        if (application == null || !RequiresProjectContract(application))
            return true;

        if (application.ProjectContract != null)
        {
            if (ProjectContractMinistryHelper.HasConfiguredLegs(application.ProjectContract))
                return true;

            if (!HasProgressBeyondOfficePreparation(application, objectSpace))
                return true;

            errorMessage = VisaUiMessages.Get("Application.ProjectContractLegsRequired");
            return false;
        }

        if (!HasProgressBeyondOfficePreparation(application, objectSpace))
            return true;

        errorMessage = VisaUiMessages.Get("ApplicationProgress.ProjectContractRequired");
        return false;
    }

    public static bool TryValidateProjectContractForProgress(
        ApplicationProgress progress,
        IObjectSpace? objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        var application = progress.Application;
        if (application == null || !RequiresProjectContract(application))
            return true;

        if (application.ProjectContract != null
            && TryValidateProjectContractOnApplication(application, objectSpace, out errorMessage))
            return errorMessage == null;

        if (application.ProjectContract != null)
            return false;

        if (IsPermittedWithoutProjectContract(progress, objectSpace))
            return true;

        errorMessage = VisaUiMessages.Get("ApplicationProgress.ProjectContractRequired");
        return false;
    }

    public static bool WouldMinistryDepthChange(
        Application application,
        ProjectContract? previousContract,
        ProjectContract? newContract)
    {
        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (route != ApplicationProgressRouteKind.ViaMinistries)
            return false;

        if (application.ApplicationType?.ShowProjectContract != true)
            return false;

        return ResolveLegCountForContract(application, previousContract)
            != ResolveLegCountForContract(application, newContract);
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

    private static int ResolveLegCountForContract(Application application, ProjectContract? contract)
    {
        if (contract == null)
            return GetMinistryLegCount(application);

        if (ReferenceEquals(application.ProjectContract, contract))
        {
            var snapshotCount = application.ApprovalLegSnapshots?
                .Count(s => !string.IsNullOrWhiteSpace(s.MinistryShortName)) ?? 0;
            if (snapshotCount > 0)
                return snapshotCount;
        }

        return ProjectContractMinistryHelper.GetLegCount(contract);
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

    private static bool IsOfficePreparationStep(ApplicationProgress progress) =>
        progress.State != null
        && progress.Location != null
        && string.Equals(progress.State.Code, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase)
        && string.Equals(progress.Location.Code, ApplicationProgressLocationCodes.AtOffice, StringComparison.OrdinalIgnoreCase);
}

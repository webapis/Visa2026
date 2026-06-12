using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves effective progress route settings for an <see cref="Application"/>
/// (type defaults, <see cref="ProjectContract"/> overrides).
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

    public static MinistryReviewDepth GetMinistryReviewDepth(Application? application)
    {
        if (application == null)
            return MinistryReviewDepth.FirstMinistryOnly;

        var route = ApplicationProgressRouteHelper.GetTypePickerRouteFilter(application);
        if (!route.HasValue)
            return MinistryReviewDepth.FirstMinistryOnly;

        if (route.Value == ApplicationProgressRouteKind.DirectToMigrationService)
            return MinistryReviewDepth.None;

        if (application.ApplicationType?.ShowProjectContract == true && application.ProjectContract != null)
        {
            return ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                route.Value,
                application.ProjectContract.MinistryReviewDepth);
        }

        return ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
            route.Value,
            application.ApplicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);
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

    public static bool TryValidateProjectContractOnApplication(
        Application? application,
        IObjectSpace? objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        if (application == null || !RequiresProjectContract(application))
            return true;

        if (application.ProjectContract != null)
            return true;

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

        if (application.ProjectContract != null)
            return true;

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

        var previousDepth = ResolveDepthForContract(application, previousContract);
        var newDepth = ResolveDepthForContract(application, newContract);
        return previousDepth != newDepth;
    }

    public static string FormatMinistryReviewDepthLabel(MinistryReviewDepth depth) =>
        depth == MinistryReviewDepth.FirstAndSecondMinistry
            ? VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.Two")
            : depth == MinistryReviewDepth.FirstMinistryOnly
                ? VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.One")
                : VisaUiMessages.Get("ApplicationProgressProfile.MinistryDepth.None");

    private static MinistryReviewDepth ResolveDepthForContract(Application application, ProjectContract? contract)
    {
        if (contract != null)
        {
            return ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
                ApplicationProgressRouteKind.ViaMinistries,
                contract.MinistryReviewDepth);
        }

        return ApplicationProgressRouteHelper.NormalizeMinistryReviewDepth(
            ApplicationProgressRouteKind.ViaMinistries,
            application.ApplicationType?.MinistryReviewDepth ?? MinistryReviewDepth.FirstMinistryOnly);
    }

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

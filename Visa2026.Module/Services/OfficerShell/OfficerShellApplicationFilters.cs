using System;
using System.Linq.Expressions;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

internal static class OfficerShellApplicationFilters
{
    public static readonly Expression<Func<ApplicationProfileInstance, bool>> IsStaged = application =>
        !application.HasLeftStagedQueue
        && (application.LatestPrimaryStateCode == null
            || application.LatestPrimaryStateCode == "OFFICE_PREPARATION"
            || application.LatestPrimaryStateCode == "DRAFT");

    public static readonly Expression<Func<ApplicationProfileInstance, bool>> IsInProcess = application =>
        application.HasLeftStagedQueue
        || (application.LatestPrimaryStateCode != null
            && application.LatestPrimaryStateCode != "OFFICE_PREPARATION"
            && application.LatestPrimaryStateCode != "DRAFT");

    public static bool IsStagedApplication(ApplicationProfileInstance? application) =>
        application != null && IsStagedState(application.HasLeftStagedQueue, application.LatestPrimaryStateCode);

    public static bool IsStagedState(bool hasLeftStagedQueue, string? latestPrimaryStateCode) =>
        !hasLeftStagedQueue
        && (latestPrimaryStateCode == null
            || latestPrimaryStateCode == "OFFICE_PREPARATION"
            || latestPrimaryStateCode == "DRAFT");

    public static bool IsInProcessState(bool hasLeftStagedQueue, string? latestPrimaryStateCode) =>
        !IsStagedState(hasLeftStagedQueue, latestPrimaryStateCode);

    public static bool IsReadyForStartProcess(ApplicationProfileInstance? application) =>
        application?.ApplicationProfile != null
        && Services.ApplicationPersonRoster.ApplicationRosterHelper.GetRosterPersonCountInMemory(application) > 0;
}

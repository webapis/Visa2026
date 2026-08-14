using System;
using System.Linq.Expressions;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

internal static class OfficerShellApplicationFilters
{
    public static readonly Expression<Func<ApplicationProfileInstance, bool>> IsStaged = application =>
        string.IsNullOrEmpty(application.ProcessNumber)
        && (application.LatestPrimaryStateCode == null
            || application.LatestPrimaryStateCode == "OFFICE_PREPARATION"
            || application.LatestPrimaryStateCode == "DRAFT");

    public static readonly Expression<Func<ApplicationProfileInstance, bool>> IsInProcess = application =>
        !string.IsNullOrEmpty(application.ProcessNumber)
        || (application.LatestPrimaryStateCode != null
            && application.LatestPrimaryStateCode != "OFFICE_PREPARATION"
            && application.LatestPrimaryStateCode != "DRAFT");

    public static bool IsStagedApplication(ApplicationProfileInstance? application) =>
        application != null && IsStagedState(application.ProcessNumber, application.LatestPrimaryStateCode);

    public static bool IsStagedState(string? processNumber, string? latestPrimaryStateCode) =>
        string.IsNullOrEmpty(processNumber)
        && (latestPrimaryStateCode == null
            || latestPrimaryStateCode == "OFFICE_PREPARATION"
            || latestPrimaryStateCode == "DRAFT");

    public static bool IsInProcessState(string? processNumber, string? latestPrimaryStateCode) =>
        !IsStagedState(processNumber, latestPrimaryStateCode);

    public static bool IsReadyForStartProcess(ApplicationProfileInstance? application) =>
        application?.ApplicationProfile != null
        && Services.ApplicationPersonRoster.ApplicationRosterHelper.GetRosterPersonCountInMemory(application) > 0;
}

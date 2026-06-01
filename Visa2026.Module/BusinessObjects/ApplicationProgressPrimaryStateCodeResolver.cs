using System;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves <see cref="Application.PrimaryStateCode"/> from the latest <see cref="ApplicationProgress"/> row.
/// </summary>
public static class ApplicationProgressPrimaryStateCodeResolver
{
    public static string? Resolve(Application? application)
    {
        if (application == null)
            return null;

        var latest = ApplicationProgressHelper.GetLatest(application.ProgressHistory);
        if (latest == null)
            return null;

        var stateCode = latest.State?.Code?.Trim();
        var locationCode = latest.Location?.Code?.Trim();

        if (string.IsNullOrEmpty(stateCode))
            return string.IsNullOrEmpty(locationCode) ? null : locationCode;

        if (string.Equals(stateCode, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase))
            return ApplicationProgressStateCodes.ProcessIssued;

        if (IsTerminalRejectOrCancel(stateCode))
            return stateCode;

        if (string.Equals(stateCode, ApplicationProgressStateCodes.IsBeingPrepared, StringComparison.OrdinalIgnoreCase))
            return ApplicationProgressStateCodes.IsBeingPrepared;

        if (!ApplicationProgressTransitionHelper.IsTerminalStateCode(stateCode)
            && string.Equals(locationCode, ApplicationProgressLocationCodes.AtOffice, StringComparison.OrdinalIgnoreCase))
            return ApplicationProgressLocationCodes.AtOffice;

        return stateCode;
    }

    private static bool IsTerminalRejectOrCancel(string stateCode) =>
        ApplicationProgressTransitionHelper.IsTerminalStateCode(stateCode)
        && !string.Equals(stateCode, ApplicationProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Localized label for the latest progress step (ListView <see cref="Application.CurrentState"/>).
    /// </summary>
    public static string? ResolveDisplayName(Application? application)
    {
        if (application == null)
            return null;

        var latest = ApplicationProgressHelper.GetLatest(application.ProgressHistory);
        if (latest == null)
            return null;

        if (latest.State != null)
        {
            var stateName = LookupLocalization.GetDisplayName(latest.State);
            if (latest.Location != null)
            {
                var locationName = LookupLocalization.GetDisplayName(latest.Location);
                return $"{stateName} @ {locationName}";
            }

            return stateName;
        }

        return latest.Location != null
            ? LookupLocalization.GetDisplayName(latest.Location)
            : null;
    }
}

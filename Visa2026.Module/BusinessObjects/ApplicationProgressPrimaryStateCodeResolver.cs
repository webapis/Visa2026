using System;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves <see cref="Application.PrimaryStateCode"/> from the latest <see cref="ApplicationProgress"/> row.
/// Empty history implies office (<see cref="ApplicationProgressStateCodes.IsBeingPrepared"/>).
/// </summary>
public static class ApplicationProgressPrimaryStateCodeResolver
{
    public static string? Resolve(Application? application)
    {
        if (application == null)
            return null;

        return ResolveFromLatest(ApplicationProgressHelper.GetLatest(application.ProgressHistory));
    }

    public static string? ResolveFromLatest(ApplicationProgress? latest)
    {
        if (latest == null)
            return ApplicationProgressStateCodes.IsBeingPrepared;

        var stateCode = latest.State?.Code?.Trim();
        return string.IsNullOrEmpty(stateCode)
            ? ApplicationProgressStateCodes.IsBeingPrepared
            : stateCode;
    }

    /// <summary>
    /// Localized label for the latest progress step (ListView <see cref="Application.CurrentState"/>).
    /// </summary>
    public static string? ResolveDisplayName(Application? application)
    {
        if (application == null)
            return null;

        return ResolveDisplayNameFromLatest(ApplicationProgressHelper.GetLatest(application.ProgressHistory));
    }

    public static string? ResolveDisplayNameFromLatest(ApplicationProgress? latest)
    {
        if (latest?.State != null)
            return LookupLocalization.GetDisplayName(latest.State);

        // Implied office — Layer B catalog label for IS_BEING_PREPARED (e.g. Ofisde).
        var implied = LookupLocalization.GetCatalogDisplayName(
            "application-state",
            ApplicationProgressStateCodes.IsBeingPrepared);
        return string.IsNullOrEmpty(implied) ? "Ofisde" : implied;
    }
}

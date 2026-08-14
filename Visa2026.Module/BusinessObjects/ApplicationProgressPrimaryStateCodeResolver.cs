using System;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves <see cref="Application.PrimaryStateCode"/> from the latest <see cref="ApplicationProfileInstanceProgress"/> row.
/// Empty history implies office (<see cref="ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared"/>).
/// </summary>
public static class ApplicationProfileInstanceProgressPrimaryStateCodeResolver
{
    public static string? Resolve(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        return ResolveFromLatest(ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory));
    }

    public static string? ResolveFromLatest(ApplicationProfileInstanceProgress? latest)
    {
        if (latest == null)
            return ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared;

        var stateCode = latest.State?.Code?.Trim();
        return string.IsNullOrEmpty(stateCode)
            ? ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared
            : stateCode;
    }

    /// <summary>
    /// Localized label for the latest progress step (ListView <see cref="Application.CurrentState"/>).
    /// </summary>
    public static string? ResolveDisplayName(ApplicationProfileInstance? application)
    {
        if (application == null)
            return null;

        return ResolveDisplayNameFromLatest(ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory));
    }

    public static string? ResolveDisplayNameFromLatest(ApplicationProfileInstanceProgress? latest)
    {
        if (latest?.State != null)
            return LookupLocalization.GetDisplayName(latest.State);

        // Implied office — Layer B catalog label for IS_BEING_PREPARED (e.g. Ofisde).
        var implied = LookupLocalization.GetCatalogDisplayName(
            "application-state",
            ApplicationProfileInstanceProgressStateCodes.IsBeingPrepared);
        return string.IsNullOrEmpty(implied) ? "Ofisde" : implied;
    }
}

using System;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Travel history is only for Registration templates (check-in / check-out / info change / reg extension).
/// </summary>
public static class ApplicationProfileTravelHistoryPolicy
{
    public static bool AllowsPersonTravelHistory(ApplicationProfile? profile) =>
        profile is { ActionFamily: ApplicationProfileActionFamily.Registration };

    public static bool AllowsPersonTravelHistory(ApplicationProfileActionFamily family) =>
        family == ApplicationProfileActionFamily.Registration;
}
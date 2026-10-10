using System;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Screen filter for the dossier Applications section. One profile belongs to one button.
/// Hemmesi shows every row. Paper does not filter.
/// </summary>
public static class PersonDossierApplicationGroups
{
    public const string Invitation = "invitation";
    public const string Visa = "visa";
    public const string WorkPermit = "workPermit";
    public const string Registration = "registration";
    public const string BorderZone = "borderZone";
    public const string BusinessTrip = "businessTrip";
    public const string All = "all";

    public static readonly string[] ButtonOrder =
    [
        Invitation,
        Visa,
        WorkPermit,
        Registration,
        BorderZone,
        BusinessTrip,
        All,
    ];

    /// <summary>
    /// Invitation wins when the profile produces an invitation, including change-invitation
    /// and the service-passport invitation (that profile's produce flag is off).
    /// Business trip and registration are their own buttons and are decided first.
    /// </summary>
    public static string Resolve(ApplicationProfile? profile)
    {
        if (profile == null)
            return string.Empty;

        if (profile.ActionFamily == ApplicationProfileActionFamily.BusinessTrip)
            return BusinessTrip;

        if (profile.ActionFamily == ApplicationProfileActionFamily.Registration)
            return Registration;

        if (profile.ProduceBorderZone || profile.CancelBorderZonePermits)
            return BorderZone;

        if (IsInvitation(profile))
            return Invitation;

        if (profile.ProduceVisa || profile.CancelVisas)
            return Visa;

        if (profile.ProduceWorkPermit || profile.CancelWorkPermits || profile.ProduceWorkLocation)
            return WorkPermit;

        return string.Empty;
    }

    public static string LabelSuffix(string groupId) => groupId switch
    {
        Invitation => "ApplicationGroup.Invitation",
        Visa => "ApplicationGroup.Visa",
        WorkPermit => "ApplicationGroup.WorkPermit",
        Registration => "ApplicationGroup.Registration",
        BorderZone => "ApplicationGroup.BorderZone",
        BusinessTrip => "ApplicationGroup.BusinessTrip",
        All => "ApplicationGroup.All",
        _ => "ApplicationGroup.All",
    };

    private static bool IsInvitation(ApplicationProfile profile)
    {
        if (profile.ProduceInvitation)
            return true;

        var code = profile.Code ?? string.Empty;
        return code.Contains("service_passport", StringComparison.OrdinalIgnoreCase)
            || code.Contains("sevice_passport", StringComparison.OrdinalIgnoreCase);
    }
}

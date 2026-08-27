using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// How many latest person-owned rows to auto-link for dated document types
/// (Passport, Visa, Invitation item, Work permit item, Border zone item).
/// Other person types stay at one current row.
/// </summary>
public static class ApplicationProfilePersonLastCount
{
    public const int Min = 1;
    public const int Max = 3;
    public const int Default = 1;

    public static int Clamp(int value) =>
        value < Min ? Default : (value > Max ? Max : value);

    /// <summary>
    /// Expected auto-link count for a kind. 0 when the profile toggle is off
    /// (no new links). Types without a Last-N control always return 1 when enabled.
    /// </summary>
    public static int For(ApplicationProfile? profile, ApplicationProfileInstancePersonLinkKind kind)
    {
        if (profile == null)
            return Default;

        return kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport =>
                profile.RequirePersonPassport ? Clamp(profile.PersonPassportLastCount) : 0,
            ApplicationProfileInstancePersonLinkKind.Visa =>
                profile.RequirePersonVisa ? Clamp(profile.PersonVisaLastCount) : 0,
            ApplicationProfileInstancePersonLinkKind.InvitationItem =>
                profile.RequirePersonInvitationItem ? Clamp(profile.PersonInvitationItemLastCount) : 0,
            ApplicationProfileInstancePersonLinkKind.WorkPermitItem =>
                profile.RequirePersonWorkPermitItem ? Clamp(profile.PersonWorkPermitItemLastCount) : 0,
            ApplicationProfileInstancePersonLinkKind.BorderZoneItem =>
                profile.RequirePersonBorderZoneItem ? Clamp(profile.PersonBorderZoneItemLastCount) : 0,
            ApplicationProfileInstancePersonLinkKind.Education =>
                profile.RequirePersonEducation ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence =>
                profile.RequirePersonAddressOfResidence ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.Position =>
                profile.RequirePersonPosition ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.WorkDuty =>
                profile.RequirePersonPosition ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.Salary =>
                profile.RequirePersonSalary ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.MedicalRecord =>
                profile.RequirePersonMedical ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.RejectionItem =>
                profile.RequirePersonRejectionItem ? Default : 0,
            ApplicationProfileInstancePersonLinkKind.TravelHistory =>
                profile.RequirePersonTravelHistory ? Default : 0,
            _ => 0,
        };
    }

    public static int For(ApplicationProfileInstance? application, ApplicationProfileInstancePersonLinkKind kind)
    {
        if (application?.ApplicationProfile != null)
            return For(application.ApplicationProfile, kind);

        return ApplicationProfileInstancePersonResolver.IsAutoLinkEnabled(application, kind) ? Default : 0;
    }

    public static bool SupportsLastCount(ApplicationProfileInstancePersonLinkKind kind) =>
        kind is ApplicationProfileInstancePersonLinkKind.Passport
            or ApplicationProfileInstancePersonLinkKind.Visa
            or ApplicationProfileInstancePersonLinkKind.InvitationItem
            or ApplicationProfileInstancePersonLinkKind.WorkPermitItem
            or ApplicationProfileInstancePersonLinkKind.BorderZoneItem;
}
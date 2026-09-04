using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Maps a placeholder pack to the profile toggle that decides whether the underlying record is
/// collected at all. A token whose record the profile never collects can never be filled, so it must
/// not be offered for mapping.
/// </summary>
public static class ApplicationProfilePlaceholderPackMap
{
    public static bool IsEnabled(ApplicationProfile profile, UserReportPlaceholderPack pack)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return pack switch
        {
            UserReportPlaceholderPack.Core => true,
            UserReportPlaceholderPack.PersonPassport => profile.RequirePersonPassport,
            UserReportPlaceholderPack.PersonVisa => profile.RequirePersonVisa,
            UserReportPlaceholderPack.PersonEducation => profile.RequirePersonEducation,
            UserReportPlaceholderPack.PersonAddressOfResidence => profile.RequirePersonAddressOfResidence,
            UserReportPlaceholderPack.PersonPosition => profile.RequirePersonPosition,
            UserReportPlaceholderPack.PersonSalary => profile.RequirePersonSalary,
            UserReportPlaceholderPack.PersonMedical => profile.RequirePersonMedical,
            UserReportPlaceholderPack.PersonInvitationItem => profile.RequirePersonInvitationItem,
            UserReportPlaceholderPack.PersonWorkPermitItem => profile.RequirePersonWorkPermitItem,
            UserReportPlaceholderPack.PersonBorderZoneItem => profile.RequirePersonBorderZoneItem,
            UserReportPlaceholderPack.PersonRejectionItem => profile.RequirePersonRejectionItem,
            UserReportPlaceholderPack.PersonTravelHistory => profile.RequirePersonTravelHistory,
            _ => false,
        };
    }
}

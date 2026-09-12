using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Maps a placeholder pack to the profile toggle that decides whether the underlying record is
/// collected at all. PersonEducation is always offered: Education lives on Person even when the
/// People & links Education tile is hidden. Other packs stay gated so uncollectable tokens are not mapped.
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
            // Education lives on Person. Cancel-visa hides the People & links tile
            // (RequirePersonEducation / AllowsPersonEducation) but sanaw still maps EGLV/EGIN/EGSP.
            UserReportPlaceholderPack.PersonEducation => true,
            UserReportPlaceholderPack.PersonAddressOfResidence => profile.RequirePersonAddressOfResidence,
            UserReportPlaceholderPack.PersonPosition => profile.RequirePersonPosition,
            UserReportPlaceholderPack.PersonSalary => profile.RequirePersonSalary,
            UserReportPlaceholderPack.PersonMedical => profile.RequirePersonMedical,
            UserReportPlaceholderPack.PersonInvitationItem => profile.RequirePersonInvitationItem,
            UserReportPlaceholderPack.PersonWorkPermitItem => profile.RequirePersonWorkPermitItem,
            UserReportPlaceholderPack.PersonBorderZoneItem => profile.RequirePersonBorderZoneItem,
            UserReportPlaceholderPack.PersonRejectionItem => profile.RequirePersonRejectionItem,
            UserReportPlaceholderPack.PersonTravelHistory =>
                ApplicationProfileTravelHistoryPolicy.AllowsPersonTravelHistory(profile)
                && profile.RequirePersonTravelHistory,
            _ => false,
        };
    }
}

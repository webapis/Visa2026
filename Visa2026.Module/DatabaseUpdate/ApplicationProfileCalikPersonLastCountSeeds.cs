using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Calik tenant Last-N seeds. Last 2 means up to two valid rows (person may have 1 or 2).
/// Passport Last 2 only on <c>pasport_change</c>. Registration passport-info-change stays Last 1.
/// </summary>
public static class ApplicationProfileCalikPersonLastCountSeeds
{
    public static void Apply(ApplicationProfileTenantCatalogRow? row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.Code))
            return;

        switch (row.Code.Trim())
        {
            case "pasport_change":
                row.PersonPassportLastCount = 2;
                break;
            case "cancel_invitation_wp":
                row.PersonInvitationItemLastCount = 2;
                row.PersonWorkPermitItemLastCount = 2;
                break;
            case "cancel_invitation":
                row.PersonInvitationItemLastCount = 2;
                break;
            case "cancel_visa_wp":
                row.PersonVisaLastCount = 2;
                row.PersonWorkPermitItemLastCount = 2;
                break;
            case "cancel_workpermit":
                row.PersonWorkPermitItemLastCount = 2;
                break;
        }

        row.PersonPassportLastCount = ApplicationProfilePersonLastCount.Clamp(row.PersonPassportLastCount);
        row.PersonVisaLastCount = ApplicationProfilePersonLastCount.Clamp(row.PersonVisaLastCount);
        row.PersonInvitationItemLastCount = ApplicationProfilePersonLastCount.Clamp(row.PersonInvitationItemLastCount);
        row.PersonWorkPermitItemLastCount = ApplicationProfilePersonLastCount.Clamp(row.PersonWorkPermitItemLastCount);
        row.PersonBorderZoneItemLastCount = ApplicationProfilePersonLastCount.Clamp(row.PersonBorderZoneItemLastCount);
    }
}
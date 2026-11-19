using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Calik tenant Last-N / address-slot seeds. Last 2 means up to two valid rows (person may have 1 or 2).
/// Passport Last 2 only on <c>pasport_change</c>. Registration passport-info-change stays Last 1.
/// Address max 2 only on <c>get_invitation</c> (slot 1 required, slot 2 optional).
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
            case "get_invitation":
                row.PersonAddressOfResidenceMaxCount = 2;
                row.RequireAddress1 = true;
                row.RequireAddress2 = true;
                break;
            case "cancel_invitation_wp":
                row.PersonInvitationItemLastCount = 2;
                row.PersonWorkPermitItemLastCount = 2;
                break;
            case "cancel_invitation":
                row.PersonInvitationItemLastCount = 2;
                break;
            case "cancel_visa":
                row.PersonVisaLastCount = 2;
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
        row.PersonAddressOfResidenceMaxCount =
            ApplicationProfilePersonAddressMaxCount.Clamp(row.PersonAddressOfResidenceMaxCount);

        // Map legacy person-step address flags onto Results Address 1/2 when unset.
        if (!row.RequireAddress1 && !row.RequireAddress2 && row.RequirePersonAddressOfResidence)
        {
            row.RequireAddress1 = true;
            row.RequireAddress2 = row.PersonAddressOfResidenceMaxCount >= 2;
        }
        else if (row.RequireAddress1 || row.RequireAddress2)
        {
            row.RequirePersonAddressOfResidence = row.RequireAddress1;
            row.PersonAddressOfResidenceMaxCount = ApplicationProfilePersonAddressMaxCount.Clamp(
                (row.RequireAddress1 ? 1 : 0) + (row.RequireAddress2 ? 1 : 0));
        }
    }
}
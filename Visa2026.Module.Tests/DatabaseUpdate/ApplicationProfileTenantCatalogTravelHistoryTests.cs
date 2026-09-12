using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileTenantCatalogTravelHistoryTests
{
    [Fact]
    public void Calik_catalog_requires_travel_history_except_business_trip_invitation_and_visa_cancel()
    {
        Assert.True(ApplicationProfileTenantCatalogLoader.TryLoadRows(out var rows));
        Assert.NotEmpty(rows);

        foreach (var row in rows)
        {
            Assert.True(
                Enum.TryParse(row.ActionFamily, ignoreCase: true, out ApplicationProfileActionFamily family),
                row.Code);
            var invitationRelated = ApplicationProfileTravelHistoryPolicy.IsInvitationRelated(
                row.ProduceInvitation,
                row.CancelInvitations,
                row.ChangeInvitations,
                row.Code);
            var visaCancel = ApplicationProfileEducationPolicy.IsVisaDocumentCancellation(
                row.CancelVisas,
                row.Code);
            if (family == ApplicationProfileActionFamily.BusinessTrip || invitationRelated || visaCancel)
                Assert.False(row.RequirePersonTravelHistory, row.Code);
            else
                Assert.True(row.RequirePersonTravelHistory, row.Code);
        }

        Assert.Contains(rows, r => r.Code == "check_in_from_abroad" && r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "change_invitation" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "get_invitation" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_invitation" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_visa" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_visa_wp" && !r.RequirePersonTravelHistory);
        Assert.Contains(rows, r => r.Code == "cancel_visa_ext" && r.RequirePersonTravelHistory);
    }

    [Fact]
    public void Mapper_turns_on_travel_history_except_business_trip_invitation_and_visa_cancel()
    {
        var issuance = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(issuance, new ApplicationType());
        Assert.Equal(ApplicationProfileActionFamily.Issuance, issuance.ActionFamily);
        Assert.True(issuance.RequirePersonTravelHistory);

        var trip = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(trip, new ApplicationType { ShowBusinessTrips = true });
        Assert.Equal(ApplicationProfileActionFamily.BusinessTrip, trip.ActionFamily);
        Assert.False(trip.RequirePersonTravelHistory);

        var invitation = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            invitation,
            new ApplicationType { CanIssueInvitation = true, Name = "App_Inv" });
        Assert.True(invitation.ProduceInvitation);
        Assert.False(invitation.RequirePersonTravelHistory);

        var cancelVisa = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(
            cancelVisa,
            new ApplicationType
            {
                Name = "App_Cancel_Visa",
                Code = "cancel_visa",
                ShowVisaIsCancelled = true,
            });
        Assert.False(cancelVisa.RequirePersonTravelHistory);
    }
}
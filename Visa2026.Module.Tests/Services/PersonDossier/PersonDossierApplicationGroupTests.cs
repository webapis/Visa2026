using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierApplicationGroupTests
{
    [Theory]
    [InlineData("get_invitation", true)]
    [InlineData("get_invitation_fm", true)]
    [InlineData("get_invitation_wp", true)]
    [InlineData("get_invitation_according_to_wp", true)]
    [InlineData("change_invitation", true)]
    public void Produces_invitation_goes_under_invitations(string code, bool produceInvitation)
    {
        var group = PersonDossierApplicationGroups.Resolve(Profile(
            code,
            produceInvitation: produceInvitation));

        Assert.Equal(PersonDossierApplicationGroups.Invitation, group);
    }

    [Fact]
    public void Service_passport_invitation_goes_under_invitations_even_when_the_flag_is_off()
    {
        var group = PersonDossierApplicationGroups.Resolve(Profile(
            "get_invitation_service_passport",
            produceInvitation: false));

        Assert.Equal(PersonDossierApplicationGroups.Invitation, group);
    }

    [Fact]
    public void Cancel_invitation_is_not_in_the_invitation_button()
    {
        var group = PersonDossierApplicationGroups.Resolve(new ApplicationProfile
        {
            Code = "cancel_invitation",
            ActionFamily = ApplicationProfileActionFamily.Cancellation,
            ProduceInvitation = false,
            CancelInvitations = true,
        });

        Assert.NotEqual(PersonDossierApplicationGroups.Invitation, group);
    }

    [Fact]
    public void Departure_and_arrival_are_business_trips()
    {
        Assert.Equal(
            PersonDossierApplicationGroups.BusinessTrip,
            PersonDossierApplicationGroups.Resolve(Profile(
                "business_trip_departure",
                family: ApplicationProfileActionFamily.BusinessTrip)));
        Assert.Equal(
            PersonDossierApplicationGroups.BusinessTrip,
            PersonDossierApplicationGroups.Resolve(Profile(
                "business_trip_arrival",
                family: ApplicationProfileActionFamily.BusinessTrip)));
    }

    [Fact]
    public void Check_in_stays_on_registration()
    {
        var group = PersonDossierApplicationGroups.Resolve(Profile(
            "check_in_from_abroad",
            family: ApplicationProfileActionFamily.Registration));

        Assert.Equal(PersonDossierApplicationGroups.Registration, group);
    }

    [Fact]
    public void Visa_and_work_permit_stay_on_their_own_buttons()
    {
        Assert.Equal(
            PersonDossierApplicationGroups.Visa,
            PersonDossierApplicationGroups.Resolve(Profile("visa_ext", produceVisa: true)));
        Assert.Equal(
            PersonDossierApplicationGroups.WorkPermit,
            PersonDossierApplicationGroups.Resolve(Profile("extend_workpermit", produceWorkPermit: true)));
    }

    private static ApplicationProfile Profile(
        string code,
        bool produceInvitation = false,
        bool produceVisa = false,
        bool produceWorkPermit = false,
        ApplicationProfileActionFamily family = ApplicationProfileActionFamily.Issuance) =>
        new()
        {
            Code = code,
            ActionFamily = family,
            ProduceInvitation = produceInvitation,
            ProduceVisa = produceVisa,
            ProduceWorkPermit = produceWorkPermit,
        };
}

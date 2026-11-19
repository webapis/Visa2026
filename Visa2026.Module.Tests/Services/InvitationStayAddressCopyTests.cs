using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class InvitationStayAddressCopyTests
{
    [Theory]
    [InlineData("get_invitation")]
    [InlineData("get_invitation_wp")]
    [InlineData("get_invitation_fm")]
    [InlineData("get_invitation_according_to_wp")]
    public void Invitation_receiving_templates_collect_the_stay(string code)
    {
        Assert.True(ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(null, code));
    }

    [Theory]
    [InlineData("get_invitation_service_passport")]
    [InlineData("work_visa")]
    [InlineData("change_invitation")]
    public void Other_templates_do_not_collect_the_stay(string code)
    {
        Assert.False(ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(null, code));
    }

    [Fact]
    public void Mapper_turns_address_and_stay_off_for_change_invitation()
    {
        var profile = new ApplicationProfile();
#pragma warning disable CS0618
        ApplicationProfileFromApplicationTypeMapper.Apply(
            profile,
            new ApplicationType { Name = "App_Change_Inv", Code = "change_invitation" });
#pragma warning restore CS0618

#pragma warning disable CS0618
        Assert.False(profile.RequireInvitationAddress);
        Assert.False(profile.RequireInvitationPlace);
        Assert.False(profile.RequirePersonAddressOfResidence);
#pragma warning restore CS0618
        Assert.True(profile.RequirePersonPosition);
    }

    [Fact]
    public void Mapper_turns_address1_on_for_caklyk_almak_not_legacy_invitation_stay()
    {
        var profile = new ApplicationProfile();
#pragma warning disable CS0618
        ApplicationProfileFromApplicationTypeMapper.Apply(
            profile,
            new ApplicationType { Name = "App_Inv", Code = "get_invitation" });
        Assert.False(profile.RequireInvitationAddress);
        Assert.False(profile.RequireInvitationPlace);
#pragma warning restore CS0618
        Assert.True(profile.RequireAddress1);
    }

    [Fact]
    public void Mapper_turns_address1_on_for_invitation_and_work_permit()
    {
        var profile = new ApplicationProfile();
#pragma warning disable CS0618
        ApplicationProfileFromApplicationTypeMapper.Apply(
            profile,
            new ApplicationType { Name = "App_Inv_And_WP", Code = "get_invitation_wp" });
        Assert.False(profile.RequireInvitationAddress);
        Assert.False(profile.RequireInvitationPlace);
#pragma warning restore CS0618
        Assert.True(profile.RequireAddress1);
    }

    [Fact]
    public void Tick_is_offered_only_when_the_linked_address_differs_from_a_complete_stay()
    {
        var region = new Region();
        var city = new City { Region = region };
        var lodging = new Lodging { City = city, FullAddress = "Çalyk ýaşaýyş jaýy" };
        var stay = new InvitationAddress
        {
            Region = region,
            City = city,
            Type = ResidenceType.Lodging,
            Lodging = lodging,
        };
        SetId(region);
        SetId(city);
        SetId(lodging);

        var same = new AddressOfResidence
        {
            Region = region,
            City = city,
            Type = ResidenceType.Lodging,
            Lodging = lodging,
        };
        var other = new AddressOfResidence
        {
            Region = region,
            City = city,
            Type = ResidenceType.Hotel,
        };

        Assert.True(InvitationStayAddressCopy.IsComplete(stay));
        Assert.True(InvitationStayAddressCopy.Matches(same, stay));
        Assert.False(InvitationStayAddressCopy.OfferTick(stay, same));
        Assert.True(InvitationStayAddressCopy.OfferTick(stay, other));
        Assert.False(InvitationStayAddressCopy.ShouldInsertFromStay(same, stay));
        Assert.True(InvitationStayAddressCopy.ShouldInsertFromStay(other, stay));
        Assert.True(InvitationStayAddressCopy.ShouldInsertFromStay(null, stay));
        Assert.False(InvitationStayAddressCopy.ShouldInsertFromStay(null, new InvitationAddress
        {
            Region = region,
            City = city,
            Type = ResidenceType.Lodging,
        }));
        Assert.False(InvitationStayAddressCopy.IsComplete(new InvitationAddress
        {
            Region = region,
            City = city,
            Type = ResidenceType.Lodging,
        }));
    }

    private static void SetId(DevExpress.Persistent.BaseImpl.EF.BaseObject entity)
    {
        entity.ID = Guid.NewGuid();
    }
}

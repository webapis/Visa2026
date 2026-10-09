using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Dual-read cutover maps deprecated ApplicationType flags onto ApplicationProfile.
/// Wrong ActionFamily / Çakylyk toggles / SLA days change officer case shape silently.
/// </summary>
public sealed class ApplicationProfileFromApplicationTypeMapperApplyTests
{
    [Fact]
    public void ResolveProfileCode_prefers_trimmed_Code_over_Name()
    {
#pragma warning disable CS0618
        var type = new ApplicationType { Code = "  get_invitation  ", Name = "App_Inv" };
#pragma warning restore CS0618

        Assert.Equal("get_invitation", ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type));
    }

    [Fact]
    public void ResolveProfileCode_slugs_Name_when_Code_blank()
    {
#pragma warning disable CS0618
        var type = new ApplicationType { Code = "   ", Name = "App_Work_Permit" };
#pragma warning restore CS0618

        Assert.Equal("app-work-permit", ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type));
    }

    [Fact]
    public void ResolveProfileCode_returns_UNKNOWN_TYPE_when_Name_and_Code_blank()
    {
#pragma warning disable CS0618
        var type = new ApplicationType { Code = null, Name = "  " };
#pragma warning restore CS0618

        Assert.Equal("UNKNOWN_TYPE", ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type));
    }

    [Fact]
    public void ResolveProfileCode_truncates_slug_to_64_chars()
    {
#pragma warning disable CS0618
        var type = new ApplicationType { Name = new string('A', 80) };
#pragma warning restore CS0618

        var code = ApplicationProfileFromApplicationTypeMapper.ResolveProfileCode(type);
        Assert.Equal(64, code.Length);
        Assert.Equal(new string('a', 64), code);
    }

    [Theory]
    [InlineData(true, false, false, false, ApplicationProfileActionFamily.Registration)]
    [InlineData(false, true, false, false, ApplicationProfileActionFamily.Cancellation)]
    [InlineData(false, false, true, false, ApplicationProfileActionFamily.Change)]
    [InlineData(false, false, false, true, ApplicationProfileActionFamily.BusinessTrip)]
    [InlineData(false, false, false, false, ApplicationProfileActionFamily.Issuance)]
    public void Apply_sets_ActionFamily_from_type_flags(
        bool showRegistrations,
        bool showVisaCancelled,
        bool showVisaChanged,
        bool showBusinessTrips,
        ApplicationProfileActionFamily expected)
    {
        var profile = new ApplicationProfile();
        var type = new ApplicationType
        {
            ShowRegistrations = showRegistrations,
            ShowVisaIsCancelled = showVisaCancelled,
            ShowVisaIsChanged = showVisaChanged,
            ShowBusinessTrips = showBusinessTrips,
        };

        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        Assert.Equal(expected, profile.ActionFamily);
    }

    [Fact]
    public void Apply_CaklykAlmak_requires_invitation_stay_and_pins_last_visa()
    {
#pragma warning disable CS0618
        var type = new ApplicationType
        {
            Name = "App_Inv",
            Code = "get_invitation",
            ShowCurrentVisa = false,
            ShowCurrentSalary = true,
            ShowCurrentMedicalRecord = true,
        };
#pragma warning restore CS0618
        var profile = new ApplicationProfile();

        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        Assert.False(profile.RequirePersonAddressOfResidence);
        Assert.True(profile.RequireInvitationAddress);
        Assert.True(profile.RequireInvitationPlace);
        Assert.True(profile.RequirePersonVisa);
        Assert.False(profile.RequirePersonSalary);
        Assert.False(profile.RequirePersonMedical);
        Assert.Equal("get_invitation", profile.Code);
    }

    [Fact]
    public void Apply_maps_produce_and_cancel_flags()
    {
        var profile = new ApplicationProfile();
        var type = new ApplicationType
        {
            CanIssueInvitation = true,
            CanIssueWorkPermit = true,
            CanIssueVisa = true,
            ShowBorderZoneLocation = true,
            ShowRejections = true,
            ShowInvitationItemIsCancelled = true,
            ShowWorkPermitItemIsChanged = true,
        };

        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        Assert.True(profile.ProduceInvitation);
        Assert.True(profile.ProduceWorkPermit);
        Assert.True(profile.ProduceVisa);
        Assert.True(profile.ProduceBorderZone);
        Assert.True(profile.ProduceRejection);
        Assert.True(profile.CancelInvitations);
        Assert.True(profile.ChangeWorkPermits);
        Assert.False(profile.CancelBorderZonePermits);
    }

    [Theory]
    [InlineData(MinistryReviewDepth.FirstMinistryOnly, 14)]
    [InlineData(MinistryReviewDepth.FirstAndSecondMinistry, 21)]
    [InlineData(MinistryReviewDepth.None, 14)]
    public void Apply_sets_ministry_SLA_from_review_depth_when_via_ministries(
        MinistryReviewDepth depth,
        int expectedDays)
    {
        var profile = new ApplicationProfile();
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistryReviewDepth = depth,
        };

        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        Assert.Equal(expectedDays, profile.MinistrySlaDays);
        Assert.Equal(14, profile.MigrationSlaDays);
    }

    [Fact]
    public void Apply_uses_default_ministry_SLA_when_route_is_direct()
    {
        var profile = new ApplicationProfile();
        var type = new ApplicationType
        {
            ApplicationProfileInstanceProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            MinistryReviewDepth = MinistryReviewDepth.FirstAndSecondMinistry,
        };

        ApplicationProfileFromApplicationTypeMapper.Apply(profile, type);

        Assert.Equal(14, profile.MinistrySlaDays);
    }

    [Theory]
    [InlineData(ApplicationTypeCategory.FamilyMember, false, true)]
    [InlineData(ApplicationTypeCategory.Both, true, true)]
    [InlineData(ApplicationTypeCategory.Employee, true, false)]
    public void Apply_sets_audience_from_category(
        ApplicationTypeCategory category,
        bool forEmployee,
        bool forFamilyMember)
    {
        var profile = new ApplicationProfile();
        ApplicationProfileFromApplicationTypeMapper.Apply(profile, new ApplicationType { Category = category });

        Assert.Equal(forEmployee, profile.ForEmployee);
        Assert.Equal(forFamilyMember, profile.ForFamilyMember);
        Assert.False(profile.ForTemporaryVisitor);
    }
}

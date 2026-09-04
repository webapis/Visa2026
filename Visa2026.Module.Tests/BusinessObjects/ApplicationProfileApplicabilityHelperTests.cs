using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationProfileApplicabilityHelperTests
{
    [Fact]
    public void IsProfileSelectable_RejectsInactiveProfiles()
    {
        var profile = new ApplicationProfile { IsActive = false, ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries };

        Assert.False(ApplicationProfileApplicabilityHelper.IsProfileSelectable(profile, null, null));
    }

    [Fact]
    public void IsProfileSelectable_FiltersByProgressRoute()
    {
        var profile = new ApplicationProfile
        {
            IsActive = true,
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };

        Assert.True(ApplicationProfileApplicabilityHelper.IsProfileSelectable(
            profile,
            null,
            ApplicationProfileInstanceProgressRouteKind.ViaMinistries));

        Assert.False(ApplicationProfileApplicabilityHelper.IsProfileSelectable(
            profile,
            null,
            ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService));
    }

    [Fact]
    public void IsProfileSelectable_AllowsEmptyApplicabilityCriteria()
    {
        var profile = new ApplicationProfile
        {
            IsActive = true,
            ApplicabilityCriteria = null,
        };

        Assert.True(ApplicationProfileApplicabilityHelper.IsProfileSelectable(profile, null, null));
    }

    [Fact]
    public void FormatActionFamily_ReturnsReadableLabels()
    {
        Assert.Equal("Issuance", ApplicationProfilePickerDisplayHelper.FormatActionFamily(ApplicationProfileActionFamily.Issuance));
        Assert.Equal("Change", ApplicationProfilePickerDisplayHelper.FormatActionFamily(ApplicationProfileActionFamily.Change));
        Assert.Equal("Business trip", ApplicationProfilePickerDisplayHelper.FormatActionFamily(ApplicationProfileActionFamily.BusinessTrip));
    }

    [Fact]
    public void FormatRelatedTo_AppendsCheckInForRegistration()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RegistrationKind = ApplicationProfileRegistrationKind.CheckIn,
        };

        Assert.Equal("Registration · Check in", ApplicationProfilePickerDisplayHelper.FormatRelatedTo(profile));
    }

    [Fact]
    public void FormatRelatedTo_AppendsInfoChangeForRegistration()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RegistrationKind = ApplicationProfileRegistrationKind.InfoChange,
        };

        Assert.Equal("Registration · Info change", ApplicationProfilePickerDisplayHelper.FormatRelatedTo(profile));
    }

    [Fact]
    public void FormatRelatedTo_AppendsRegExtensionForRegistration()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RegistrationKind = ApplicationProfileRegistrationKind.Extension,
        };

        Assert.Equal("Registration · Reg extension", ApplicationProfilePickerDisplayHelper.FormatRelatedTo(profile));
    }

    [Theory]
    [InlineData("App_Reg_Check_In", ApplicationProfileRegistrationKind.CheckIn)]
    [InlineData("App_Reg_Check_In_Internal", ApplicationProfileRegistrationKind.CheckIn)]
    [InlineData("App_Reg_Check_Out", ApplicationProfileRegistrationKind.CheckOut)]
    [InlineData("App_Reg_Check_Out_Internal", ApplicationProfileRegistrationKind.CheckOut)]
    [InlineData("App_Reg_Info_Change_Passport", ApplicationProfileRegistrationKind.InfoChange)]
    [InlineData("App_Reg_Info_Change_Visa", ApplicationProfileRegistrationKind.InfoChange)]
    [InlineData("App_Reg_ext", ApplicationProfileRegistrationKind.Extension)]
    public void InferRegistrationKind_FromTypeName(string typeName, ApplicationProfileRegistrationKind expected) =>
        Assert.Equal(expected, ApplicationProfileRegistrationKindHelper.InferFromApplicationTypeName(typeName));

    [Fact]
    public void Resolve_ClearsKindWhenNotRegistration() =>
        Assert.Equal(
            ApplicationProfileRegistrationKind.None,
            ApplicationProfileRegistrationKindHelper.Resolve(
                ApplicationProfileActionFamily.Issuance,
                ApplicationProfileRegistrationKind.CheckIn));

    [Fact]
    public void ApplyRegistrationPersonDefaults_TurnsPositionOn()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RequirePersonPosition = false,
        };

        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);
        Assert.True(profile.RequirePersonPosition);
        Assert.False(profile.RequireUrgency);
        Assert.Null(profile.DefaultUrgency);
    }

    [Fact]
    public void ApplyRegistrationPersonDefaults_ClearsUrgency()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
            RequireUrgency = true,
        };

        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);
        Assert.False(profile.RequireUrgency);
        Assert.Null(profile.DefaultUrgency);
    }

    [Fact]
    public void ApplyRegistrationPersonDefaults_LeavesIssuanceAlone()
    {
        var profile = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Issuance,
            RequirePersonPosition = false,
        };

        ApplicationProfileRegistrationKindHelper.ApplyRegistrationPersonDefaults(profile);
        Assert.False(profile.RequirePersonPosition);
    }
}

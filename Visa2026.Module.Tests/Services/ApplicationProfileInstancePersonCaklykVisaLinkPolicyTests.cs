using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Çakylyk Almak must pin the latest issued visa on the invitation form even when expired.
/// </summary>
public sealed class ApplicationProfileInstancePersonCaklykVisaLinkPolicyTests
{
    private static readonly DateTime Today = DateTime.Today;

    [Fact]
    public void LinksLastIssuedVisaIncludingExpired_true_for_get_invitation_code()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { Code = "get_invitation" },
        };

        Assert.True(ApplicationProfileInstancePersonValidItems.LinksLastIssuedVisaIncludingExpired(application));
    }

    [Fact]
    public void LinksLastIssuedVisaIncludingExpired_true_for_App_Inv_type_name()
    {
#pragma warning disable CS0618
        var application = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { Name = "App_Inv" },
        };
#pragma warning restore CS0618

        Assert.True(ApplicationProfileInstancePersonValidItems.LinksLastIssuedVisaIncludingExpired(application));
    }

    [Fact]
    public void LinksLastIssuedVisaIncludingExpired_false_for_unrelated_profile()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { Code = "get_visa" },
        };

        Assert.False(ApplicationProfileInstancePersonValidItems.LinksLastIssuedVisaIncludingExpired(application));
        Assert.False(ApplicationProfileInstancePersonValidItems.LinksLastIssuedVisaIncludingExpired(null));
    }

    [Fact]
    public void CanAutoLink_expired_visa_allowed_only_when_Caklyk_policy_applies()
    {
        var expired = new Visa
        {
            StartDate = Today.AddDays(-40),
            ExpirationDate = Today.AddDays(-5),
        };
#pragma warning disable CS0618
        var viaType = new ApplicationProfileInstance
        {
            ApplicationType = new ApplicationType { Name = "App_Inv" },
        };
#pragma warning restore CS0618

        Assert.True(ApplicationProfileInstancePersonValidItems.CanAutoLink(
            viaType,
            ApplicationProfileInstancePersonLinkKind.Visa,
            expired));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanAutoLink(
            viaType,
            ApplicationProfileInstancePersonLinkKind.Passport,
            expired));
        Assert.False(ApplicationProfileInstancePersonValidItems.CanLinkVisa(expired, includeExpired: false));
        Assert.True(ApplicationProfileInstancePersonValidItems.CanLinkVisa(expired, includeExpired: true));
    }
}

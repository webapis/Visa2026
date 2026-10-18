#nullable enable

using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// New invitations copy visa category/period from the case summary and keep 90-day letter expiry rules.
/// </summary>
public sealed class InvitationCaseSummaryDefaultTests
{
    [Fact]
    public void OnCreated_prefers_case_summary_visa_category_and_period()
    {
        var category = new VisaCategory { NameTm = "Case category" };
        var period = new VisaPeriod { NameTm = "Case period" };
        var invitation = new Invitation
        {
            ApplicationProfileInstance = new ApplicationProfileInstance
            {
                VisaCategory = category,
                VisaPeriod = period,
            },
        };

        invitation.OnCreated();

        Assert.Same(category, invitation.VisaCategory);
        Assert.Same(period, invitation.VisaPeriod);
    }

    [Fact]
    public void OnCreated_keeps_officer_preset_category_and_period()
    {
        var presetCategory = new VisaCategory { NameTm = "Preset category" };
        var presetPeriod = new VisaPeriod { NameTm = "Preset period" };
        var invitation = new Invitation
        {
            VisaCategory = presetCategory,
            VisaPeriod = presetPeriod,
            ApplicationProfileInstance = new ApplicationProfileInstance
            {
                VisaCategory = new VisaCategory { NameTm = "Case category" },
                VisaPeriod = new VisaPeriod { NameTm = "Case period" },
            },
        };

        invitation.OnCreated();

        Assert.Same(presetCategory, invitation.VisaCategory);
        Assert.Same(presetPeriod, invitation.VisaPeriod);
    }

    [Fact]
    public void OnCreated_derives_expiration_from_preset_issued_date()
    {
        var issued = new DateTime(2026, 1, 10);
        var invitation = new Invitation { IssuedDate = issued };

        invitation.OnCreated();

        Assert.Equal(issued, invitation.IssuedDate);
        Assert.Equal(Invitation.ExpirationFromIssued(issued), invitation.ExpirationDate);
    }

    [Fact]
    public void OnCreated_does_not_overwrite_preset_expiration_date()
    {
        var issued = new DateTime(2026, 3, 1);
        var expiration = new DateTime(2026, 4, 15);
        var invitation = new Invitation
        {
            IssuedDate = issued,
            ExpirationDate = expiration,
        };

        invitation.OnCreated();

        Assert.Equal(expiration, invitation.ExpirationDate);
    }
}

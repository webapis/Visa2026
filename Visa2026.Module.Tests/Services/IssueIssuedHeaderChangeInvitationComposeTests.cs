using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssueIssuedHeaderChangeInvitationComposeTests
{
    [Fact]
    public void IsChangeInvitationCase_TrueWhenChangeInvitationsFlag()
    {
        var instance = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ChangeInvitations = true, Code = "other" },
        };

        Assert.True(IssueIssuedHeaderComposeService.IsChangeInvitationCase(instance));
    }

    [Fact]
    public void IsChangeInvitationCase_TrueWhenChangeInvitationCode()
    {
        var instance = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ChangeInvitations = false, Code = "change_invitation" },
        };

        Assert.True(IssueIssuedHeaderComposeService.IsChangeInvitationCase(instance));
    }

    [Fact]
    public void IsChangeInvitationCase_FalseForOrdinaryInvitationProfile()
    {
        var instance = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ChangeInvitations = false, Code = "get_invitation" },
        };

        Assert.False(IssueIssuedHeaderComposeService.IsChangeInvitationCase(instance));
    }

    [Fact]
    public void ApplyChangeInvitationSourceDefaults_CopiesExactExpiryCategoryPeriodAndBorderIncludingYok()
    {
        var category = new VisaCategory { ID = Guid.NewGuid() };
        var period = new VisaPeriod { ID = Guid.NewGuid() };
        var source = new Invitation
        {
            ExpirationDate = new DateTime(2027, 1, 14),
            VisaCategory = category,
            VisaPeriod = period,
            BorderZoneLocation = BorderZoneSelectionHelper.NoneValue,
        };

        DateTime? expiration = new DateTime(2026, 12, 1);
        Guid? categoryId = Guid.NewGuid();
        Guid? periodId = Guid.NewGuid();
        var border = "Ahal";

        IssueIssuedHeaderComposeService.ApplyChangeInvitationSourceDefaults(
            source,
            ref expiration,
            ref categoryId,
            ref periodId,
            ref border);

        Assert.Equal(new DateTime(2027, 1, 14), expiration);
        Assert.Equal(category.ID, categoryId);
        Assert.Equal(period.ID, periodId);
        Assert.Equal(BorderZoneSelectionHelper.NoneValue, border);
    }

    [Fact]
    public void ApplyChangeInvitationSourceDefaults_NoOpWhenSourceMissing()
    {
        DateTime? expiration = new DateTime(2026, 12, 1);
        Guid? categoryId = Guid.NewGuid();
        Guid? periodId = Guid.NewGuid();
        var border = "Ahal";
        var originalCategory = categoryId;
        var originalPeriod = periodId;

        IssueIssuedHeaderComposeService.ApplyChangeInvitationSourceDefaults(
            null,
            ref expiration,
            ref categoryId,
            ref periodId,
            ref border);

        Assert.Equal(new DateTime(2026, 12, 1), expiration);
        Assert.Equal(originalCategory, categoryId);
        Assert.Equal(originalPeriod, periodId);
        Assert.Equal("Ahal", border);
    }
}
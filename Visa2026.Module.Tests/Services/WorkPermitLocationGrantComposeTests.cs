using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class WorkPermitLocationGrantComposeTests
{
    [Fact]
    public void IsWorkPermitLocationGrantCardComplete_RequiresPassportAndLocationsOnly()
    {
        var row = new IssueIssuedHeaderPersonLineDraft { PersonName = "Serdar" };
        Assert.False(IssueIssuedHeaderComposeService.IsWorkPermitLocationGrantCardComplete(row));

        row.PassportId = Guid.NewGuid();
        Assert.False(IssueIssuedHeaderComposeService.IsWorkPermitLocationGrantCardComplete(row));

        row.WorkPermittedLocations = "Mary";
        Assert.True(IssueIssuedHeaderComposeService.IsWorkPermitLocationGrantCardComplete(row));
    }

    [Fact]
    public void TryResolveKind_ResolvesWorkPermitLocationGrantCatalogKey()
    {
        Assert.True(IssueIssuedHeaderComposeService.TryResolveKind(
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant,
            out var kind));
        Assert.Equal(IssueIssuedHeaderKind.WorkPermitLocationGrant, kind);
        Assert.Equal(
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant,
            IssueIssuedHeaderComposeService.CatalogKeyFor(kind));
    }

    [Fact]
    public void CaseWorkPermitLocationCopy_EnabledForProduceWorkLocationWithoutProduceWorkPermit()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceWorkPermit = false,
                ProduceWorkLocation = true,
                RequireWorkPermitLocation = true,
                RequirePersonWorkPermitItem = true,
            },
        };

        Assert.True(CaseWorkPermitLocationCopy.IsEnabled(application));
        Assert.True(CaseWorkPermitLocationCopy.ShowInLinkReview(application));
    }

    [Fact]
    public void ShowWorkPermitLocationGrants_FollowsProduceWorkLocation()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceWorkLocation = true },
        };
        Assert.True(ApplicationProfileConfigurationResolver.ShowWorkPermitLocationGrants(application));
        Assert.True(ApplicationTypeCapabilities.CanIssueWorkPermitLocationGrant(application));

        application.ApplicationProfile.ProduceWorkLocation = false;
        Assert.False(ApplicationProfileConfigurationResolver.ShowWorkPermitLocationGrants(application));
    }
}
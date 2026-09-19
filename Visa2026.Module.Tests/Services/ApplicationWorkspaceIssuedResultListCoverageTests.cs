using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceIssuedResultListCoverageTests
{
    [Fact]
    public void Build_hides_types_the_profile_does_not_produce()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceWorkPermit = false,
                ProduceVisa = true,
                ProduceRejection = true,
                ProduceBorderZone = false,
            },
        };

        var chips = ApplicationWorkspaceIssuedResultListCoverage.Build(
            application,
            rosterCount: 2,
            coverageByKey: new Dictionary<string, int>
            {
                [ApplicationWorkspaceIssuedRecordsCatalog.Invitation] = 2,
                [ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit] = 2,
                [ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa] = 1,
                [ApplicationWorkspaceIssuedRecordsCatalog.Rejection] = 0,
            });

        Assert.Equal(3, chips.Count);
        Assert.Equal(ApplicationWorkspaceIssuedRecordsCatalog.Invitation, chips[0].Key);
        Assert.Equal(2, chips[0].CoverageCount);
        Assert.Equal(2, chips[0].ExpectedCount);
        Assert.Equal("ok", ApplicationWorkspaceIssuedResultListCoverage.Tone(chips[0]));
        Assert.Equal(ApplicationWorkspaceIssuedRecordsCatalog.Rejection, chips[1].Key);
        Assert.True(chips[1].IsOptional);
        Assert.Equal(0, chips[1].ExpectedCount);
        Assert.Equal("opt", ApplicationWorkspaceIssuedResultListCoverage.Tone(chips[1]));
        Assert.Equal(ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, chips[2].Key);
        Assert.Equal(1, chips[2].CoverageCount);
        Assert.Equal(2, chips[2].ExpectedCount);
        Assert.Equal("miss", ApplicationWorkspaceIssuedResultListCoverage.Tone(chips[2]));
    }

    [Fact]
    public void Build_empty_when_profile_produces_nothing()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile(),
        };

        var chips = ApplicationWorkspaceIssuedResultListCoverage.Build(application, 3, null);

        Assert.Empty(chips);
        Assert.Equal("—", ApplicationWorkspaceIssuedResultListCoverage.FormatDisplay(chips));
    }

    [Fact]
    public void RatioText_optional_is_count_only()
    {
        var optional = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.Rejection, "Rejection", 0, 0, true);
        var required = new ApplicationWorkspaceIssuedResultListCoverage.Chip(
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation, "Invitation", 2, 2, false);

        Assert.Equal("0", ApplicationWorkspaceIssuedResultListCoverage.RatioText(optional));
        Assert.Equal("2 / 2", ApplicationWorkspaceIssuedResultListCoverage.RatioText(required));
    }
}

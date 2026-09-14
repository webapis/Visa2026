using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceCancelCountsTests
{
    [Fact]
    public void Resolve_PrefersLinkedVisaCountOverCurrentPlusNext()
    {
        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.Resolve(linkedCount: 2, mergeLineCurrentAndNextCount: 1));
    }

    [Fact]
    public void Resolve_FallsBackToCurrentPlusNextWhenNoLinks()
    {
        Assert.Equal(1, ApplicationProfileInstanceCancelCounts.Resolve(linkedCount: 0, mergeLineCurrentAndNextCount: 1));
    }

    [Fact]
    public void FromCurrentAndNextVisa_CountsBothSlotsOnOneLine()
    {
        var lines = new List<ApplicationRosterMergeLine>
        {
            new()
            {
                CurrentVisa = new Visa { VisaNumber = "A14886414" },
                NextVisa = new Visa { VisaNumber = "A17327411" },
            },
        };

        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.FromCurrentAndNextVisa(lines));
    }

    [Fact]
    public void CountLinked_CountsDistinctVisaPins()
    {
        var application = new ApplicationProfileInstance
        {
            PersonResolvedLinks =
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                    LinkedObjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.Visa,
                    LinkedObjectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                },
            ],
        };

        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.CountLinked(
            application, ApplicationProfileInstancePersonLinkKind.Visa));
        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.Visas(application));
    }

    [Fact]
    public void FromCurrentAndPreviousWorkPermit_CountsBothSlotsOnOneLine()
    {
        var lines = new List<ApplicationRosterMergeLine>
        {
            new()
            {
                CurrentWorkPermitItem = new WorkPermitItem { WorkPermitNumber = "WP-1" },
                PreviousWorkPermitItem = new WorkPermitItem { WorkPermitNumber = "WP-2" },
            },
        };

        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.FromCurrentAndPreviousWorkPermit(lines));
    }

    [Fact]
    public void CountLinked_CountsDistinctWorkPermitPins()
    {
        var application = new ApplicationProfileInstance
        {
            PersonResolvedLinks =
            [
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
                    LinkedObjectId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                },
                new ApplicationProfileInstancePersonResolvedLink
                {
                    LinkKind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
                    LinkedObjectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                },
            ],
        };

        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.CountLinked(
            application, ApplicationProfileInstancePersonLinkKind.WorkPermitItem));
        Assert.Equal(2, ApplicationProfileInstanceCancelCounts.WorkPermits(application));
    }

    [Fact]
    public void FromCurrentInvitationHeaders_TwoPeopleSameInvitation_CountOne()
    {
        var invitation = new Invitation
        {
            ID = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            InvitationNumber = "COO-1",
        };
        var lines = new List<ApplicationRosterMergeLine>
        {
            new() { CurrentInvitationItem = new InvitationItem { Invitation = invitation } },
            new() { CurrentInvitationItem = new InvitationItem { Invitation = invitation } },
        };

        Assert.Equal(1, ApplicationProfileInstanceCancelCounts.FromCurrentInvitationHeaders(lines));
    }
}
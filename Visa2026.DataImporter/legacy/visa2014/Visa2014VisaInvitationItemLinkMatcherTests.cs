using System;
using System.Collections.Generic;
using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014VisaInvitationItemLinkMatcherTests
{
    [Fact]
    public void SelectClosest_PicksSmallestGapBeforeVisaIssueDate()
    {
        var person = Guid.NewGuid();
        var app = Guid.NewGuid();
        var near = Guid.NewGuid();
        var far = Guid.NewGuid();
        var invNear = Guid.NewGuid();
        var invFar = Guid.NewGuid();

        var chosen = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            new DateTime(2026, 8, 1),
            [
                Candidate(far, invFar, person, app, new DateTime(2026, 1, 1), new DateTime(2025, 12, 1)),
                Candidate(near, invNear, person, app, new DateTime(2026, 7, 20), new DateTime(2026, 7, 1)),
            ],
            new HashSet<Guid>());

        Assert.Equal(near, chosen);
    }

    [Fact]
    public void SelectClosest_PrefersSoftChronologyThenFallsBack()
    {
        var person = Guid.NewGuid();
        var app = Guid.NewGuid();
        var softOk = Guid.NewGuid();
        var softFail = Guid.NewGuid();

        var chosenPreferred = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            new DateTime(2026, 8, 1),
            [
                Candidate(softFail, Guid.NewGuid(), person, app, new DateTime(2026, 6, 1), new DateTime(2026, 7, 1)),
                Candidate(softOk, Guid.NewGuid(), person, app, new DateTime(2026, 7, 10), new DateTime(2026, 7, 1)),
            ],
            new HashSet<Guid>());
        Assert.Equal(softOk, chosenPreferred);

        var chosenFallback = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            new DateTime(2026, 8, 1),
            [
                Candidate(softFail, Guid.NewGuid(), person, app, new DateTime(2026, 6, 1), new DateTime(2026, 7, 1)),
            ],
            new HashSet<Guid>());
        Assert.Equal(softFail, chosenFallback);
    }

    [Fact]
    public void SelectClosest_ExcludesCancelledChangedUsedAndAlreadyLinked()
    {
        var person = Guid.NewGuid();
        var app = Guid.NewGuid();
        var good = Guid.NewGuid();
        var linked = Guid.NewGuid();

        var chosen = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            new DateTime(2026, 8, 1),
            [
                Candidate(Guid.NewGuid(), Guid.NewGuid(), person, app, new DateTime(2026, 7, 1), new DateTime(2026, 6, 1), isCancelled: true),
                Candidate(Guid.NewGuid(), Guid.NewGuid(), person, app, new DateTime(2026, 7, 2), new DateTime(2026, 6, 1), isChanged: true),
                Candidate(Guid.NewGuid(), Guid.NewGuid(), person, app, new DateTime(2026, 7, 3), new DateTime(2026, 6, 1), isUsed: true),
                Candidate(linked, Guid.NewGuid(), person, app, new DateTime(2026, 7, 4), new DateTime(2026, 6, 1)),
                Candidate(good, Guid.NewGuid(), person, app, new DateTime(2026, 7, 5), new DateTime(2026, 6, 1)),
            ],
            new HashSet<Guid> { linked });

        Assert.Equal(good, chosen);
    }

    [Fact]
    public void SelectClosest_WithoutIssueDate_PicksLatestIssuedDate()
    {
        var person = Guid.NewGuid();
        var app = Guid.NewGuid();
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();

        var chosen = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            default,
            [
                Candidate(older, Guid.NewGuid(), person, app, new DateTime(2026, 1, 1), new DateTime(2025, 12, 1)),
                Candidate(newer, Guid.NewGuid(), person, app, new DateTime(2026, 7, 1), new DateTime(2026, 6, 1)),
            ],
            new HashSet<Guid>());

        Assert.Equal(newer, chosen);
    }

    [Fact]
    public void SelectClosest_ReturnsNullWhenNoEligibleCandidate()
    {
        var person = Guid.NewGuid();
        var app = Guid.NewGuid();

        var chosen = Visa2014VisaInvitationItemLinkMatcher.SelectClosest(
            person,
            app,
            new DateTime(2026, 8, 1),
            [
                Candidate(Guid.NewGuid(), Guid.NewGuid(), person, Guid.NewGuid(), new DateTime(2026, 7, 1), new DateTime(2026, 6, 1)),
                Candidate(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), app, new DateTime(2026, 7, 1), new DateTime(2026, 6, 1)),
            ],
            new HashSet<Guid>());

        Assert.Null(chosen);
    }

    private static Visa2014VisaInvitationItemLinkCandidate Candidate(
        Guid itemId,
        Guid invitationId,
        Guid personId,
        Guid applicationId,
        DateTime issuedDate,
        DateTime applicationDate,
        bool isCancelled = false,
        bool isChanged = false,
        bool isUsed = false) =>
        new()
        {
            InvitationItemId = itemId,
            InvitationId = invitationId,
            PersonId = personId,
            ApplicationProfileInstanceId = applicationId,
            IssuedDate = issuedDate,
            ApplicationDate = applicationDate,
            IsCancelled = isCancelled,
            IsChanged = isChanged,
            IsUsed = isUsed,
        };
}
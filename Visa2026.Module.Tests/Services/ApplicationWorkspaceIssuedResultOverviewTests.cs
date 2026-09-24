using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceIssuedResultOverviewTests
{
    [Fact]
    public void ExpectedFor_roster_on_required_zero_on_optional()
    {
        Assert.Equal(14, ApplicationWorkspaceIssuedResultOverview.ExpectedFor(false, 14));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.ExpectedFor(true, 14));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.ExpectedFor(false, -1));
    }

    [Fact]
    public void Missing_ignores_optional_and_clamps()
    {
        Assert.Equal(13, ApplicationWorkspaceIssuedResultOverview.Missing(1, 14, false));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.Missing(0, 14, true));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.Missing(16, 14, false));
    }

    [Fact]
    public void Sum_uses_coverage_and_skips_optional_rejection()
    {
        var tiles = new List<ApplicationWorkspaceCaseIssuedTile>
        {
            new() { Key = "workPermit", Count = 1, CoverageCount = 14, ExpectedCount = 14, IsOptional = false },
            new() { Key = "rejection", Count = 1, CoverageCount = 3, ExpectedCount = 0, IsOptional = true },
            new() { Key = "issuedVisa", Count = 11, CoverageCount = 11, ExpectedCount = 14, IsOptional = false },
        };

        var totals = ApplicationWorkspaceIssuedResultOverview.Sum(tiles);

        Assert.Equal(1, totals.Issued);
        Assert.Equal(1, totals.Missing);
        Assert.Equal(2, totals.Expected);
        Assert.Equal(50, ApplicationWorkspaceIssuedResultOverview.CompletenessPercent(totals));
        Assert.Equal(100, ApplicationWorkspaceIssuedResultOverview.CoveragePercent(tiles[0]));
        Assert.Equal(100, ApplicationWorkspaceIssuedResultOverview.CoveragePercent(tiles[1]));
        Assert.Equal(79, ApplicationWorkspaceIssuedResultOverview.CoveragePercent(tiles[2]));
    }

    [Fact]
    public void CountCoveredPeople_uses_items_not_headers()
    {
        var personA = new Person { ID = Guid.NewGuid() };
        var personB = new Person { ID = Guid.NewGuid() };
        var application = new ApplicationProfileInstance
        {
            WorkPermits = new ObservableCollection<WorkPermit>
            {
                new()
                {
                    WorkPermitItems = new ObservableCollection<WorkPermitItem>
                    {
                        new() { Person = personA },
                        new() { Person = personB },
                    },
                },
            },
            Invitations = new ObservableCollection<Invitation>
            {
                new()
                {
                    InvitationItems = new ObservableCollection<InvitationItem>
                    {
                        new() { Person = personA },
                    },
                },
            },
            Rejections = new ObservableCollection<Rejection>
            {
                new()
                {
                    RejectionItems = new ObservableCollection<RejectionItem>
                    {
                        new() { Person = personA },
                        new() { Person = personB },
                    },
                },
            },
            IssuedVisas = new ObservableCollection<Visa>
            {
                new() { Passport = new Passport { Person = personA } },
            },
        };

        Assert.Equal(2, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, application, null, 1));
        Assert.Equal(1, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation, application, null, 1));
        Assert.Equal(2, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.Rejection, application, null, 1));
        Assert.Equal(1, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, application, null, 1));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, application, null, 1));
    }

    [Fact]
    public void CountCoveredPeople_border_zone_uses_items()
    {
        var personA = new Person { ID = Guid.NewGuid() };
        var personB = new Person { ID = Guid.NewGuid() };
        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { personA, personB },
            BorderZones = new ObservableCollection<BorderZone>
            {
                new()
                {
                    BorderZoneItems = new ObservableCollection<BorderZoneItem>
                    {
                        new() { Person = personA },
                        new() { Person = personB },
                    },
                },
            },
        };

        Assert.Equal(2, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, application, null, 1));
    }

    [Fact]
    public void CountCoveredPeople_ignores_people_not_on_roster()
    {
        var onRoster = new Person { ID = Guid.NewGuid() };
        var extra = new Person { ID = Guid.NewGuid() };
        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { onRoster },
            WorkPermits = new ObservableCollection<WorkPermit>
            {
                new()
                {
                    WorkPermitItems = new ObservableCollection<WorkPermitItem>
                    {
                        new() { Person = onRoster },
                        new() { Person = extra },
                    },
                },
            },
            IssuedVisas = new ObservableCollection<Visa>
            {
                new() { Passport = new Passport { Person = extra } },
            },
        };

        Assert.Equal(1, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, application, null, 2));
        Assert.Equal(0, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, application, null, 1));
    }

    [Fact]
    public void RosterPersonIds_removes_seretmezlik_excluded()
    {
        var active = new Person { ID = Guid.NewGuid() };
        var excluded = new Person { ID = Guid.NewGuid() };
        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { active, excluded },
            Exclusions = new ObservableCollection<ApplicationProfileInstanceExclusion>
            {
                new()
                {
                    LetterNumber = "01/-01",
                    LetterDate = new DateTime(2026, 1, 15),
                    People = new ObservableCollection<ApplicationProfileInstanceExclusionPerson>
                    {
                        new() { PersonId = excluded.ID, Person = excluded },
                    },
                },
            },
        };

        var roster = ApplicationWorkspaceIssuedResultOverview.RosterPersonIds(application, null);

        Assert.Single(roster);
        Assert.Contains(active.ID, roster);
        Assert.DoesNotContain(excluded.ID, roster);
    }

    [Fact]
    public void CountCoveredPeople_ignores_issued_docs_for_excluded_people()
    {
        var active = new Person { ID = Guid.NewGuid() };
        var excluded = new Person { ID = Guid.NewGuid() };
        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { active, excluded },
            Exclusions = new ObservableCollection<ApplicationProfileInstanceExclusion>
            {
                new()
                {
                    People = new ObservableCollection<ApplicationProfileInstanceExclusionPerson>
                    {
                        new() { PersonId = excluded.ID },
                    },
                },
            },
            Invitations = new ObservableCollection<Invitation>
            {
                new()
                {
                    InvitationItems = new ObservableCollection<InvitationItem>
                    {
                        new() { Person = active },
                        new() { Person = excluded },
                    },
                },
            },
        };

        Assert.Equal(1, ApplicationWorkspaceIssuedResultOverview.CountCoveredPeople(
            ApplicationWorkspaceIssuedRecordsCatalog.Invitation, application, null, 2));
    }

    [Fact]
    public void CompletenessPercent_is_100_when_expected_is_zero()
    {
        Assert.Equal(100, ApplicationWorkspaceIssuedResultOverview.CompletenessPercent(new(0, 0, 0)));
    }
}
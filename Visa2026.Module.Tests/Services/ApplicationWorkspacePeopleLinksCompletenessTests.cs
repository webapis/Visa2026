using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspacePeopleLinksCompletenessTests
{
    [Fact]
    public void Resolve_empty_roster_is_empty()
    {
        var view = new ApplicationWorkspaceCaseView
        {
            People = new List<ApplicationWorkspaceCasePerson>(),
        };

        Assert.Equal(ApplicationWorkspacePeopleLinksCompleteness.NavStatus.EmptyRoster, ApplicationWorkspacePeopleLinksCompleteness.Resolve(view));
        Assert.Equal(0, ApplicationWorkspacePeopleLinksCompleteness.PeopleWithGaps(view));
    }

    [Fact]
    public void Resolve_person_with_short_salary_is_incomplete()
    {
        var view = View(
            Person("A", Record("passport", 1, 1), Record("salary", 0, 1)));

        Assert.Equal(ApplicationWorkspacePeopleLinksCompleteness.NavStatus.Incomplete, ApplicationWorkspacePeopleLinksCompleteness.Resolve(view));
        Assert.Equal(1, ApplicationWorkspacePeopleLinksCompleteness.PeopleWithGaps(view));
        Assert.True(ApplicationWorkspacePeopleLinksCompleteness.IsKindShort(view.People[0], "salary"));
        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsKindShort(view.People[0], "passport"));
        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsKindShort(view.People[0], "visa"));
    }

    [Fact]
    public void Resolve_all_required_tiles_filled_is_complete()
    {
        var view = View(
            Person("A", Record("passport", 1, 1), Record("visa", 1, 1), Record("salary", 1, 1)),
            Person("B", Record("passport", 2, 2), Record("visa", 1, 1)));

        Assert.Equal(ApplicationWorkspacePeopleLinksCompleteness.NavStatus.Complete, ApplicationWorkspacePeopleLinksCompleteness.Resolve(view));
        Assert.Equal(0, ApplicationWorkspacePeopleLinksCompleteness.PeopleWithGaps(view));
    }

    [Fact]
    public void PeopleWithGaps_counts_people_not_tiles()
    {
        var view = View(
            Person("A", Record("visa", 0, 1), Record("salary", 0, 1)),
            Person("B", Record("visa", 0, 1)));

        Assert.Equal(2, ApplicationWorkspacePeopleLinksCompleteness.PeopleWithGaps(view));
    }

    [Fact]
    public void IsCountShort_matches_expected_count()
    {
        Assert.True(ApplicationWorkspacePeopleLinksCompleteness.IsCountShort(0, 1));
        Assert.True(ApplicationWorkspacePeopleLinksCompleteness.IsCountShort(1, 2));
        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsCountShort(1, 1));
        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsCountShort(2, 1));
    }

    [Fact]
    public void IsKindShort_ignores_kinds_not_on_the_person()
    {
        var person = Person("A", Record("passport", 1, 1));

        Assert.False(ApplicationWorkspacePeopleLinksCompleteness.IsKindShort(person, "visa"));
    }

    private static ApplicationWorkspaceCaseView View(params ApplicationWorkspaceCasePerson[] people) =>
        new() { People = people };

    private static ApplicationWorkspaceCasePerson Person(string name, params ApplicationWorkspaceCasePersonRecord[] records) =>
        new() { Name = name, Records = records };

    private static ApplicationWorkspaceCasePersonRecord Record(string key, int count, int expected) =>
        new() { Key = key, Count = count, ExpectedCount = expected };
}
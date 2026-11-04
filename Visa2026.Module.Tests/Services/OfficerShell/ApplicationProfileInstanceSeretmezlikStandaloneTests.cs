using System;
using System.Collections.ObjectModel;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

public class ApplicationProfileInstanceSeretmezlikStandaloneTests
{
    [Fact]
    public void Build_IncludesTargetApplicationWhenRequired()
    {
        var profile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true };
        var target = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            FullApplicationNumber = "M/R/-1807",
            ApplicationProfile = new ApplicationProfile { Name = "Wiza uzaltmak" },
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            TargetApplicationProfileInstance = target,
            TargetApplicationProfileInstanceId = target.ID,
        };

        var fields = ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, null);
        var field = Assert.Single(fields, f => f.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.TargetApplicationProfileInstance);

        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldKind.Lookup, field.Kind);
        Assert.Equal(target.ID, field.SelectedId);
        Assert.Contains("M/R/-1807", field.DisplayValue, StringComparison.Ordinal);
    }

    [Fact]
    public void TargetLookupOption_SearchTextIncludesNumberAndPeopleNames()
    {
        var option = new ApplicationWorkspaceLookupOption
        {
            Id = Guid.NewGuid(),
            DisplayName = "101/-001 · Çakylyk Almak",
            Detail = "Enes Can Uzun, Celil Kocaeli",
            SearchText = "101/-001 101/-001 · Çakylyk Almak Enes Can Uzun, Celil Kocaeli",
        };

        Assert.Contains("101/-001", option.SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enes Can Uzun", option.SearchText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Celil Kocaeli", option.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShowTargetApplicationProfileInstance_FollowsRequireFlag()
    {
        var profile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true };
        var app = new ApplicationProfileInstance { ApplicationProfile = profile };
        Assert.True(ApplicationProfileConfigurationResolver.ShowTargetApplicationProfileInstance(app));
        Assert.True(ApplicationProfileConfigurationResolver.IsSeretmezlikInstance(app));

        profile.RequireTargetApplicationProfileInstance = false;
        Assert.False(ApplicationProfileConfigurationResolver.ShowTargetApplicationProfileInstance(app));
    }

    [Fact]
    public void GetExcludedPersonIds_UsesLegacyExclusionsWithoutObjectSpace()
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

        var ids = ApplicationProfileInstanceExclusionQueries.GetExcludedPersonIds(application, objectSpace: null);
        Assert.Single(ids);
        Assert.Contains(excluded.ID, ids);
    }

    [Fact]
    public void BuildMergeDataFromSeretmezlikInstance_UsesCaseNumberAndRosterPeople()
    {
        var target = new ApplicationProfileInstance
        {
            FullApplicationNumber = "TGT-1",
            People = new ObservableCollection<Person>
            {
                new() { ID = Guid.NewGuid(), LastName = "A", FirstName = "One" },
                new() { ID = Guid.NewGuid(), LastName = "B", FirstName = "Two" },
            },
        };
        var person = new Person { ID = Guid.NewGuid(), LastName = "Uzun", FirstName = "Enes", MiddleName = "Can" };
        var seretmezlik = new ApplicationProfileInstance
        {
            FullApplicationNumber = "SER-9",
            ApplicationDate = new DateTime(2026, 2, 1),
            TargetApplicationProfileInstance = target,
            TargetApplicationProfileInstanceId = target.ID,
            People = new ObservableCollection<Person> { person },
            ApplicationProfile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true },
        };

        var data = ApplicationProfileInstanceExclusionLetterBuilder.BuildMergeDataFromSeretmezlikInstance(
            seretmezlik,
            target,
            objectSpace: null,
            letterDetails: new ApplicationProfileInstanceExclusionDraft
            {
                AddresseeName = "Türkmenistanyň Döwlet migrasiýa gullugyna",
                Subject = "test subject",
                OriginalRosterCount = 2,
            });

        Assert.Equal("SER-9", data["LetterNumber"]);
        Assert.Equal("TGT-1", data["ApplicationNumber"]);
        Assert.Equal("2", data["OriginalRosterCount"]);
        Assert.Equal("1", data["ExcludedCount"]);
        var people = Assert.IsAssignableFrom<System.Collections.IEnumerable>(data["People"]);
        Assert.Single(people.Cast<object>());
    }

    [Fact]
    public void CanCreate_AlwaysRefuses_DeprecatedInCaseLetters()
    {
        var service = new ApplicationProfileInstanceExclusionService(progress: null!);
        var instance = new ApplicationProfileInstance();

        Assert.False(service.CanCreate(instance, out var reason));
        Assert.Contains("Application Profile Instance", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryValidateLinkPerson_RequiresTargetAndTargetRosterMembership()
    {
        var person = new Person { ID = Guid.NewGuid() };
        var seretmezlik = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true },
        };

        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.TryValidateLinkPerson(
            objectSpace: null!, seretmezlik, person, out var missingTarget));
        Assert.Contains("target", missingTarget, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetLinkableTargetPersonIds_EmptyWhenTargetUnset()
    {
        var seretmezlik = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true },
        };

        Assert.Empty(ApplicationProfileInstanceSeretmezlikRosterHelper.GetLinkableTargetPersonIds(
            objectSpace: null!,
            seretmezlik));
    }
}
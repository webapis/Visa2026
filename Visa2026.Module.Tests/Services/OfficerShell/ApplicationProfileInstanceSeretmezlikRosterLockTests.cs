using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

/// <summary>
/// Seretmezlik roster lock gates: target + people make the target immutable, and
/// people already on the Seretmezlik roster cannot be unlinked (irreversible exclusion).
/// </summary>
public class ApplicationProfileInstanceSeretmezlikRosterLockTests
{
    private static ApplicationProfileInstance Seretmezlik(Guid? targetId = null, params Person[] people)
    {
        var app = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { RequireTargetApplicationProfileInstance = true },
            TargetApplicationProfileInstanceId = targetId,
            People = new ObservableCollection<Person>(people),
        };
        return app;
    }

    [Fact]
    public void IsSeretmezlik_DelegatesToConfigurationResolver()
    {
        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsSeretmezlik(null));
        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsSeretmezlik(
            new ApplicationProfileInstance { ApplicationProfile = new ApplicationProfile() }));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.IsSeretmezlik(Seretmezlik()));
    }

    [Fact]
    public void IsTargetLocked_RequiresSeretmezlikTargetAndAtLeastOnePerson()
    {
        var targetId = Guid.NewGuid();
        var person = new Person { ID = Guid.NewGuid() };

        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsTargetLocked(null));
        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsTargetLocked(Seretmezlik()));
        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsTargetLocked(Seretmezlik(targetId)));
        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.IsTargetLocked(
            Seretmezlik(Guid.Empty, person)));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.IsTargetLocked(
            Seretmezlik(targetId, person)));
    }

    [Fact]
    public void CanUnlinkPerson_BlocksPeopleAlreadyOnSeretmezlikRoster()
    {
        var linked = new Person { ID = Guid.NewGuid() };
        var other = new Person { ID = Guid.NewGuid() };
        var seretmezlik = Seretmezlik(Guid.NewGuid(), linked);

        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(null, linked));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(
            new ApplicationProfileInstance { ApplicationProfile = new ApplicationProfile() },
            linked));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(seretmezlik, null));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(
            seretmezlik, new Person { ID = Guid.Empty }));

        Assert.False(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(seretmezlik, linked));
        Assert.True(ApplicationProfileInstanceSeretmezlikRosterHelper.CanUnlinkPerson(seretmezlik, other));
    }
}

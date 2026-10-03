using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Person master delete must detach case roster rows; early gates and ignore-lock unlink
/// prevent required-field blocks when Officers delete from Employees.
/// </summary>
public class ApplicationPersonDetachForDeleteGateTests
{
    [Fact]
    public void DetachForPersonDelete_noops_on_null_or_empty_id()
    {
        var space = EmptyQueryObjectSpaceStub.Create();
        ApplicationProfileInstancePersonService.DetachForPersonDelete(null!, new Person());
        ApplicationProfileInstancePersonService.DetachForPersonDelete(space, null!);
        ApplicationProfileInstancePersonService.DetachForPersonDelete(space, new Person());
    }

    [Fact]
    public void DetachForPersonDelete_with_empty_queries_does_not_throw()
    {
        var space = EmptyQueryObjectSpaceStub.Create();
        var person = new Person();
        typeof(Person).GetProperty("ID")!.SetValue(person, Guid.NewGuid());

        ApplicationProfileInstancePersonService.DetachForPersonDelete(space, person);
    }

    [Fact]
    public void UnlinkPerson_ignoreRosterLock_removes_person_from_case_people()
    {
        var space = EmptyQueryObjectSpaceStub.Create();
        var personId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var person = new Person
        {
            ApplicationProfileInstances = new ObservableCollection<ApplicationProfileInstance>(),
        };
        typeof(Person).GetProperty("ID")!.SetValue(person, personId);

        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { person },
        };
        typeof(ApplicationProfileInstance).GetProperty("ID")!.SetValue(application, applicationId);
        person.ApplicationProfileInstances.Add(application);

        ApplicationProfileInstancePersonService.UnlinkPerson(space, application, person, ignoreRosterLock: true);

        Assert.Empty(application.People);
        Assert.Empty(person.ApplicationProfileInstances!);
    }

    [Fact]
    public void UnlinkPerson_respects_null_gates()
    {
        var space = EmptyQueryObjectSpaceStub.Create();
        var person = new Person();
        typeof(Person).GetProperty("ID")!.SetValue(person, Guid.NewGuid());
        var application = new ApplicationProfileInstance
        {
            People = new ObservableCollection<Person> { person },
        };
        typeof(ApplicationProfileInstance).GetProperty("ID")!.SetValue(application, Guid.NewGuid());

        ApplicationProfileInstancePersonService.UnlinkPerson(space, null!, person, ignoreRosterLock: false);
        ApplicationProfileInstancePersonService.UnlinkPerson(space, application, null!, ignoreRosterLock: false);
        Assert.Single(application.People);
    }
}

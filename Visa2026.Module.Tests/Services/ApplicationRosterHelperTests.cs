using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationRosterHelperTests
{
    [Fact]
    public void IsPersonOnApplication_TrueWhenPeopleContainsPerson()
    {
        var personId = Guid.NewGuid();
        var person = new Person();
        person.ID = personId;
        var application = new ApplicationProfileInstance
        {
            People = new List<Person> { person },
        };

        Assert.True(ApplicationRosterHelper.IsPersonOnApplication(application, person));
    }

    [Fact]
    public void IsPersonOnApplication_FalseWhenPeopleEmptyAndNoObjectSpace()
    {
        var person = new Person();
        person.ID = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ID = Guid.NewGuid(),
            People = new List<Person>(),
        };

        Assert.False(ApplicationRosterHelper.IsPersonOnApplication(application, person, objectSpace: null));
    }
}
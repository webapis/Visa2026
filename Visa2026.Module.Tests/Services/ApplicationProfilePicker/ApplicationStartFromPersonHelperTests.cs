using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationProfilePicker;

/// <summary>
/// Person → Application start gates that do not require a live ObjectSpace query.
/// </summary>
public class ApplicationStartFromPersonHelperTests
{
    [Fact]
    public void Validate_missing_context_is_blocked()
    {
        var result = ApplicationStartFromPersonHelper.Validate(
            objectSpace: null!,
            profile: null!,
            seedPerson: null!,
            selectedPeople: Array.Empty<Person>());

        Assert.True(result.IsBlocked);
        Assert.Contains(result.Errors, e => e.Contains("Missing profile or person", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetPeopleCandidates_null_inputs_return_empty()
    {
        Assert.Empty(ApplicationStartFromPersonHelper.GetPeopleCandidates(null!, new Person(), new ApplicationProfile()));
        Assert.Empty(ApplicationStartFromPersonHelper.GetPeopleCandidates(
            null!,
            seedPerson: null!,
            profile: null!));
    }

    [Fact]
    public void HasOpenApplication_null_or_empty_person_id_is_false()
    {
        var profile = new ApplicationProfile { ID = Guid.NewGuid() };
        var person = new Person { ID = Guid.Empty };

        Assert.False(ApplicationStartFromPersonHelper.HasOpenApplication(null!, person, profile));
        Assert.False(ApplicationStartFromPersonHelper.HasOpenApplication(null!, null!, profile));
        Assert.False(ApplicationStartFromPersonHelper.HasOpenApplication(null!, person, null!));
    }

    [Fact]
    public void Validate_null_profile_or_seed_is_blocked_even_with_people()
    {
        var people = new List<Person> { new() { FirstName = "A", LastName = "B" } };

        var missingProfile = ApplicationStartFromPersonHelper.Validate(
            objectSpace: null!,
            profile: null!,
            seedPerson: new Person { FirstName = "Seed", LastName = "Person" },
            selectedPeople: people);
        Assert.True(missingProfile.IsBlocked);

        var missingSeed = ApplicationStartFromPersonHelper.Validate(
            objectSpace: null!,
            profile: new ApplicationProfile(),
            seedPerson: null!,
            selectedPeople: people);
        Assert.True(missingSeed.IsBlocked);
    }
}

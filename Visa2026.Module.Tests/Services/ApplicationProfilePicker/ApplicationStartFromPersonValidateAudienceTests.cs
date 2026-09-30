using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services.ApplicationProfilePicker;

/// <summary>
/// Validate gates that need a non-null <see cref="DevExpress.ExpressApp.IObjectSpace"/>
/// but short-circuit <c>HasOpenApplication</c> via empty person ids (no query).
/// </summary>
public class ApplicationStartFromPersonValidateAudienceTests
{
    [Fact]
    public void Validate_empty_selection_is_blocked()
    {
        var space = QueryableObjectSpaceStub.Create();
        var profile = EmployeeProfile();
        var seed = Person("Seed", PersonRecordRole.Employee);

        var result = ApplicationStartFromPersonHelper.Validate(space, profile, seed, Array.Empty<Person>());

        Assert.True(result.IsBlocked);
        Assert.Contains(result.Errors, e => e.Contains("at least one person", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_audience_mismatch_is_blocked()
    {
        var space = QueryableObjectSpaceStub.Create();
        var profile = EmployeeProfile();
        var seed = Person("Seed", PersonRecordRole.Employee);
        var visitor = Person("Visitor", PersonRecordRole.TemporaryVisitor);

        var result = ApplicationStartFromPersonHelper.Validate(space, profile, seed, [visitor]);

        Assert.True(result.IsBlocked);
        Assert.Contains(result.Errors, e => e.Contains("audience", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_via_ministry_requires_project_contract_on_seed()
    {
        var space = QueryableObjectSpaceStub.Create();
        var profile = EmployeeProfile();
        profile.ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries;
        var seed = Person("Seed", PersonRecordRole.Employee);
        var selected = Person("Seed", PersonRecordRole.Employee);

        var result = ApplicationStartFromPersonHelper.Validate(space, profile, seed, [selected]);

        Assert.True(result.IsBlocked);
        Assert.Contains(result.Errors, e => e.Contains("Project contract", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_via_ministry_blocks_people_on_other_contracts()
    {
        var space = QueryableObjectSpaceStub.Create();
        var profile = EmployeeProfile();
        profile.ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries;

        var contractA = new ProjectContract { ID = Guid.NewGuid(), Name = "A" };
        var contractB = new ProjectContract { ID = Guid.NewGuid(), Name = "B" };
        var seed = Person("Seed", PersonRecordRole.Employee);
        seed.ProjectContract = contractA;
        var other = Person("Other", PersonRecordRole.Employee);
        other.ProjectContract = contractB;

        var result = ApplicationStartFromPersonHelper.Validate(space, profile, seed, [other]);

        Assert.True(result.IsBlocked);
        Assert.Contains(result.Errors, e => e.Contains("same Project contract", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_flags_incomplete_required_passport_as_warning()
    {
        var space = QueryableObjectSpaceStub.Create();
        var profile = EmployeeProfile();
        profile.RequirePersonPassport = true;
        profile.PersonPassportLastCount = 1;
        var seed = Person("Seed", PersonRecordRole.Employee);
        var selected = Person("Seed", PersonRecordRole.Employee);

        var result = ApplicationStartFromPersonHelper.Validate(space, profile, seed, [selected]);

        Assert.False(result.IsBlocked);
        Assert.Contains(result.Warnings, w => w.Contains("lack required valid data", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(result.FlaggedIncompletePeople);
    }

    private static ApplicationProfile EmployeeProfile() =>
        new()
        {
            ID = Guid.NewGuid(),
            Name = "Employee register",
            ForEmployee = true,
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };

    private static Person Person(string firstName, PersonRecordRole role) =>
        new()
        {
            // Empty ID keeps HasOpenApplication from querying the stub.
            ID = Guid.Empty,
            FirstName = firstName,
            LastName = "Test",
            PersonRole = role,
        };
}

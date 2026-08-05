using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonRoleHelperTests
{
    [Fact]
    public void CriteriaConstants_UseExpectedEnumPaths()
    {
        Assert.Contains("PersonRecordRole,Employee", PersonRoleHelper.EmployeeCriteria);
        Assert.Contains("PersonRecordRole,FamilyMember", PersonRoleHelper.FamilyMemberCriteria);
        Assert.Contains("PersonRecordRole,TemporaryVisitor", PersonRoleHelper.TemporaryVisitorCriteria);
        Assert.Contains("!=", PersonRoleHelper.NotEmployeeCriteria);
        Assert.Equal(PersonRoleHelper.EmployeeCriteria, PersonRoleHelper.IsEmployeeRoleCriteria);
    }

    [Fact]
    public void ApplyRole_Employee_SetsIsEmployeeAndKeepsFamilyLinks()
    {
        var sponsor = new Person();
        var relationship = new Relationship { NameTm = "aýaly" };
        var person = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            IsEmployee = false,
            SponsoringEmployee = sponsor,
            Relationship = relationship,
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);

        Assert.Equal(PersonRecordRole.Employee, person.PersonRole);
        Assert.True(person.IsEmployee);
        Assert.Same(sponsor, person.SponsoringEmployee);
        Assert.Same(relationship, person.Relationship);
    }

    [Fact]
    public void ApplyRole_FamilyMember_ClearsIsEmployeeOnly()
    {
        var person = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            IsEmployee = true,
            SponsoringEmployee = new Person(),
            Relationship = new Relationship { NameTm = "ogly" },
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);

        Assert.Equal(PersonRecordRole.FamilyMember, person.PersonRole);
        Assert.False(person.IsEmployee);
        Assert.NotNull(person.SponsoringEmployee);
        Assert.NotNull(person.Relationship);
    }

    [Fact]
    public void ApplyRole_TemporaryVisitor_ClearsFamilyLinksAndIsEmployee()
    {
        var person = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            IsEmployee = true,
            SponsoringEmployee = new Person(),
            Relationship = new Relationship { NameTm = "gyzy" },
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.TemporaryVisitor);

        Assert.Equal(PersonRecordRole.TemporaryVisitor, person.PersonRole);
        Assert.False(person.IsEmployee);
        Assert.Null(person.SponsoringEmployee);
        Assert.Null(person.Relationship);
    }

    [Fact]
    public void SyncIsEmployee_ReflectsCurrentRole()
    {
        var person = new Person { PersonRole = PersonRecordRole.Employee, IsEmployee = false };
        PersonRoleHelper.SyncIsEmployee(person);
        Assert.True(person.IsEmployee);

        person.PersonRole = PersonRecordRole.FamilyMember;
        PersonRoleHelper.SyncIsEmployee(person);
        Assert.False(person.IsEmployee);
    }

    [Fact]
    public void ClearFamilyMemberLinks_NullsSponsorAndRelationship()
    {
        var person = new Person
        {
            SponsoringEmployee = new Person(),
            Relationship = new Relationship { NameTm = "aýaly" },
        };

        PersonRoleHelper.ClearFamilyMemberLinks(person);

        Assert.Null(person.SponsoringEmployee);
        Assert.Null(person.Relationship);
    }
}

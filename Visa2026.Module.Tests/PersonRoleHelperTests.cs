using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonRoleHelperTests
{
    [Fact]
    public void CriteriaConstants_MatchEnumTokens()
    {
        Assert.Contains("PersonRecordRole,Employee", PersonRoleHelper.EmployeeCriteria);
        Assert.Contains("PersonRecordRole,FamilyMember", PersonRoleHelper.FamilyMemberCriteria);
        Assert.Contains("PersonRecordRole,TemporaryVisitor", PersonRoleHelper.TemporaryVisitorCriteria);
        Assert.Equal(PersonRoleHelper.EmployeeCriteria, PersonRoleHelper.IsEmployeeRoleCriteria);
        Assert.StartsWith("PersonRole !=", PersonRoleHelper.NotEmployeeCriteria);
    }

    [Fact]
    public void ApplyRole_Employee_SetsRoleAndIsEmployee()
    {
        var person = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            IsEmployee = false,
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee);

        Assert.Equal(PersonRecordRole.Employee, person.PersonRole);
        Assert.True(person.IsEmployee);
    }

    [Fact]
    public void ApplyRole_FamilyMember_ClearsIsEmployeeButKeepsLinks()
    {
        var sponsor = new Person();
        var relationship = new Relationship();
        var person = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            IsEmployee = true,
            SponsoringEmployee = sponsor,
            Relationship = relationship,
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.FamilyMember);

        Assert.Equal(PersonRecordRole.FamilyMember, person.PersonRole);
        Assert.False(person.IsEmployee);
        Assert.Same(sponsor, person.SponsoringEmployee);
        Assert.Same(relationship, person.Relationship);
    }

    [Fact]
    public void ApplyRole_TemporaryVisitor_ClearsFamilyMemberLinks()
    {
        var person = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = new Person(),
            Relationship = new Relationship(),
        };

        PersonRoleHelper.ApplyRole(person, PersonRecordRole.TemporaryVisitor);

        Assert.Equal(PersonRecordRole.TemporaryVisitor, person.PersonRole);
        Assert.False(person.IsEmployee);
        Assert.Null(person.SponsoringEmployee);
        Assert.Null(person.Relationship);
    }

    [Fact]
    public void SyncIsEmployee_MirrorsEmployeeRoleOnly()
    {
        var person = new Person { PersonRole = PersonRecordRole.Employee, IsEmployee = false };
        PersonRoleHelper.SyncIsEmployee(person);
        Assert.True(person.IsEmployee);

        person.PersonRole = PersonRecordRole.FamilyMember;
        PersonRoleHelper.SyncIsEmployee(person);
        Assert.False(person.IsEmployee);
    }
}

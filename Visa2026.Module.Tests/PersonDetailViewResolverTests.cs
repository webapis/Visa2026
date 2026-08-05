using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests;

public sealed class PersonDetailViewResolverTests
{
    [Theory]
    [InlineData("Person_ListView_Employees", PersonDetailViewIds.Employee)]
    [InlineData("Person_ListView_FamilyMembers", PersonDetailViewIds.FamilyMember)]
    [InlineData("Person_ListView_TemporaryVisitors", PersonDetailViewIds.TemporaryVisitor)]
    public void Resolve_PrefersTypedListViewIdOverPersonRole(string listViewId, string expectedDetailViewId)
    {
        var person = new Person { PersonRole = PersonRecordRole.FamilyMember };

        Assert.Equal(expectedDetailViewId, PersonDetailViewResolver.Resolve(listViewId, person));
    }

    [Theory]
    [InlineData(PersonRecordRole.Employee, PersonDetailViewIds.Employee)]
    [InlineData(PersonRecordRole.TemporaryVisitor, PersonDetailViewIds.TemporaryVisitor)]
    [InlineData(PersonRecordRole.FamilyMember, PersonDetailViewIds.FamilyMember)]
    public void Resolve_FallsBackToPersonRoleWhenListViewUnknown(PersonRecordRole role, string expectedDetailViewId)
    {
        var person = new Person { PersonRole = role };

        Assert.Equal(expectedDetailViewId, PersonDetailViewResolver.Resolve("Person_ListView", person));
        Assert.Equal(expectedDetailViewId, PersonDetailViewResolver.Resolve(null, person));
        Assert.Equal(expectedDetailViewId, PersonDetailViewResolver.Resolve(string.Empty, person));
    }

    [Fact]
    public void Resolve_ListViewOverrideIgnoresConflictingRole()
    {
        var visitor = new Person { PersonRole = PersonRecordRole.TemporaryVisitor };

        Assert.Equal(
            PersonDetailViewIds.Employee,
            PersonDetailViewResolver.Resolve("Person_ListView_Employees", visitor));
    }
}

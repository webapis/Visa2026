using Visa2026.Module;
using Xunit;

namespace Visa2026.Module.Tests;

/// <summary>
/// Person DetailView nested issued-document tabs stay read-only and typed-view aware.
/// </summary>
public sealed class PersonNestedCollectionLayoutTests
{
    [Fact]
    public void ReadOnlyNestedListViewIds_IncludeIssuedDocumentCollections()
    {
        Assert.Contains(
            PersonNestedCollectionLayout.ApplicationItemsListView,
            PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
        Assert.Contains(
            PersonNestedCollectionLayout.WorkPermitItemsListView,
            PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
        Assert.Contains(
            PersonNestedCollectionLayout.InvitationItemsListView,
            PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
        Assert.Contains(
            PersonNestedCollectionLayout.RejectionItemsListView,
            PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
        Assert.Contains(
            PersonNestedCollectionLayout.FamilyMembersListView,
            PersonNestedCollectionLayout.ReadOnlyNestedListViewIds);
        Assert.Equal(5, PersonNestedCollectionLayout.ReadOnlyNestedListViewIds.Length);
    }

    [Fact]
    public void TypedDetailViewIds_IncludeEmployeeFamilyAndVisitor()
    {
        Assert.Contains(PersonDetailViewIds.Employee, PersonNestedCollectionLayout.TypedDetailViewIds);
        Assert.Contains(PersonDetailViewIds.FamilyMember, PersonNestedCollectionLayout.TypedDetailViewIds);
        Assert.Contains(PersonDetailViewIds.TemporaryVisitor, PersonNestedCollectionLayout.TypedDetailViewIds);
    }
}

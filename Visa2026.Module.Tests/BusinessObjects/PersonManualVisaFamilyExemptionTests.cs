using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Covers Relationship save exemption when the sponsor already stores manual visa family lines
/// (manual-only family model — stub FamilyMembers rows without Relationship).
/// </summary>
public sealed class PersonManualVisaFamilyExemptionTests
{
    [Fact]
    public void IsExempt_False_ForNonFamilyMemberOrMissingSponsor()
    {
        var employee = new Person { PersonRole = PersonRecordRole.Employee };
        Assert.False(employee.IsExemptFromRelationshipWhenManualVisaFamily);

        var orphan = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = null,
        };
        Assert.False(orphan.IsExemptFromRelationshipWhenManualVisaFamily);
    }

    [Fact]
    public void IsExempt_False_WhenSponsorManualTextEmptyOrYok()
    {
        var sponsorEmpty = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = null,
            FamilyMembers = new ObservableCollection<Person>(),
        };
        var fmEmpty = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsorEmpty,
        };
        sponsorEmpty.FamilyMembers.Add(fmEmpty);
        Assert.False(fmEmpty.IsExemptFromRelationshipWhenManualVisaFamily);

        var sponsorYok = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = VisaFamilyMemberLinesHelper.NoneValue,
            FamilyMembers = new ObservableCollection<Person>(),
        };
        var fmYok = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsorYok,
        };
        sponsorYok.FamilyMembers.Add(fmYok);
        Assert.False(fmYok.IsExemptFromRelationshipWhenManualVisaFamily);
    }

    [Fact]
    public void IsExempt_True_WhenSponsorHasManualLinesAndNoSiblingWithRelationship()
    {
        var sponsor = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = "Ayşe Yılmaz; 12.10.1989; aýaly; TUR",
            FamilyMembers = new ObservableCollection<Person>(),
        };
        var stub = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsor,
            Relationship = null,
        };
        var otherStub = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsor,
            Relationship = null,
        };
        sponsor.FamilyMembers.Add(stub);
        sponsor.FamilyMembers.Add(otherStub);

        Assert.True(stub.IsExemptFromRelationshipWhenManualVisaFamily);
        Assert.False(stub.RequiresRelationshipOnSave);
    }

    [Fact]
    public void IsExempt_False_WhenSiblingFamilyMemberAlreadyHasRelationship()
    {
        var spouseRel = new Relationship { NameTm = "aýaly" };
        var sponsor = new Person
        {
            PersonRole = PersonRecordRole.Employee,
            VisaApplicationFamilyMembersText = "Ayşe Yılmaz; 12.10.1989; aýaly; TUR",
            FamilyMembers = new ObservableCollection<Person>(),
        };
        var linked = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsor,
            Relationship = spouseRel,
        };
        var stub = new Person
        {
            PersonRole = PersonRecordRole.FamilyMember,
            SponsoringEmployee = sponsor,
            Relationship = null,
        };
        sponsor.FamilyMembers.Add(linked);
        sponsor.FamilyMembers.Add(stub);

        Assert.False(stub.IsExemptFromRelationshipWhenManualVisaFamily);
        Assert.True(stub.RequiresRelationshipOnSave);
    }
}

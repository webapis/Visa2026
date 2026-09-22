using System;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Covers Visa RuleFromBoolProperty graph checks that gate save (person match, invitation link, chronology).
/// </summary>
public sealed class VisaIssuingValidationRulesTests
{
    private static Person Person(string id) => new() { ID = Guid.Parse(id) };

    private static ApplicationType Type(bool canIssueInvitation = false, bool canIssueVisa = true) =>
        new()
        {
            CanIssueInvitation = canIssueInvitation,
            CanIssueVisa = canIssueVisa
        };

    private static ApplicationItem IssuingItem(DateTime applicationDate, Person person, ApplicationType type)
    {
        var application = new Application
        {
            ApplicationDate = applicationDate,
            ApplicationType = type
        };
        var item = new ApplicationItem
        {
            ID = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Person = person,
            Application = application
        };
        return item;
    }

    [Fact]
    public void IsPersonValid_TrueWhenIssuingItemOrPassportPersonMissing()
    {
        var visa = new Visa();
        Assert.True(visa.IsPersonValid);

        visa.IssuingApplicationItem = new ApplicationItem { Person = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") };
        Assert.True(visa.IsPersonValid);
    }

    [Fact]
    public void IsPersonValid_RequiresMatchingPersonOnIssuingItemAndPassport()
    {
        var personA = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var personB = Person("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var type = Type();
        var item = IssuingItem(new DateTime(2026, 1, 1, 0, 0, 0), personA, type);

        var visa = new Visa
        {
            IssuingApplicationItem = item,
            Passport = new Passport { Person = personA }
        };
        Assert.True(visa.IsPersonValid);

        visa.Passport = new Passport { Person = personB };
        Assert.False(visa.IsPersonValid);
    }

    [Fact]
    public void IsInvitationPersonValid_RequiresMatchingPersonOnInvitationItemAndPassport()
    {
        var personA = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var personB = Person("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var invitationItem = new InvitationItem { Person = personA };
        var visa = new Visa
        {
            InvitationItem = invitationItem,
            Passport = new Passport { Person = personA }
        };
        Assert.True(visa.IsInvitationPersonValid);

        visa.Passport = new Passport { Person = personB };
        Assert.False(visa.IsInvitationPersonValid);
    }

    [Fact]
    public void IsInvitationLinkConsistent_AllowsInvitationOnlyWhenTypeCanIssueInvitation()
    {
        var person = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var invitationItem = new InvitationItem { Person = person };

        var visa = new Visa
        {
            InvitationItem = invitationItem,
            IssuingApplicationItem = IssuingItem(
                new DateTime(2026, 1, 1, 0, 0, 0),
                person,
                Type(canIssueInvitation: false, canIssueVisa: true))
        };
        Assert.False(visa.IsInvitationLinkConsistent);

        visa.IssuingApplicationItem = IssuingItem(
            new DateTime(2026, 1, 1, 0, 0, 0),
            person,
            Type(canIssueInvitation: true, canIssueVisa: true));
        Assert.True(visa.IsInvitationLinkConsistent);
    }

    [Fact]
    public void IsInvitationLinkConsistent_TrueWhenInvitationOrIssuingItemMissing()
    {
        var visa = new Visa();
        Assert.True(visa.IsInvitationLinkConsistent);

        visa.InvitationItem = new InvitationItem();
        Assert.True(visa.IsInvitationLinkConsistent);
    }

    [Fact]
    public void IsIssuingChronologyValid_TrueWhenIssueDateUnsetOrNoLinkedSources()
    {
        var visa = new Visa();
        Assert.True(visa.IsIssuingChronologyValid);

        visa.IssueDate = new DateTime(2026, 2, 1, 0, 0, 0);
        Assert.True(visa.IsIssuingChronologyValid);
    }

    [Fact]
    public void IsIssuingChronologyValid_RequiresIssueAfterApplicationDate()
    {
        var person = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var item = IssuingItem(new DateTime(2026, 1, 10, 0, 0, 0), person, Type());

        var visa = new Visa { IssuingApplicationItem = item };
        visa.IssueDate = new DateTime(2026, 1, 11, 0, 0, 0);
        Assert.True(visa.IsIssuingChronologyValid);

        visa.IssueDate = new DateTime(2026, 1, 10, 0, 0, 0);
        Assert.False(visa.IsIssuingChronologyValid);

        visa.IssueDate = new DateTime(2026, 1, 9, 0, 0, 0);
        Assert.False(visa.IsIssuingChronologyValid);
    }

    [Fact]
    public void IsIssuingChronologyValid_WithInvitation_RequiresIssueAfterIssuedAndIssuedAfterApplication()
    {
        var person = Person("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var item = IssuingItem(new DateTime(2026, 1, 1, 0, 0, 0), person, Type(canIssueInvitation: true));

        var invitation = new Invitation { IssuedDate = new DateTime(2026, 1, 15, 0, 0, 0) };
        var invitationItem = new InvitationItem
        {
            Person = person,
            Invitation = invitation
        };

        var visa = new Visa
        {
            IssuingApplicationItem = item,
            InvitationItem = invitationItem
        };

        // App 1 Jan < Invitation 15 Jan < Visa 20 Jan
        visa.IssueDate = new DateTime(2026, 1, 20, 0, 0, 0);
        Assert.True(visa.IsIssuingChronologyValid);

        // Visa not after invitation
        visa.IssueDate = new DateTime(2026, 1, 15, 0, 0, 0);
        Assert.False(visa.IsIssuingChronologyValid);

        // Invitation not after application
        invitation.IssuedDate = new DateTime(2025, 12, 31, 0, 0, 0);
        visa.IssueDate = new DateTime(2026, 1, 20, 0, 0, 0);
        Assert.False(visa.IsIssuingChronologyValid);
    }

    [Fact]
    public void IsIssuingChronologyValid_InvitationOnly_RequiresIssueAfterIssuedDate()
    {
        var invitation = new Invitation { IssuedDate = new DateTime(2026, 3, 1, 0, 0, 0) };
        var visa = new Visa
        {
            InvitationItem = new InvitationItem { Invitation = invitation }
        };

        visa.IssueDate = new DateTime(2026, 3, 2, 0, 0, 0);
        Assert.True(visa.IsIssuingChronologyValid);

        visa.IssueDate = new DateTime(2026, 3, 1, 0, 0, 0);
        Assert.False(visa.IsIssuingChronologyValid);
    }
}

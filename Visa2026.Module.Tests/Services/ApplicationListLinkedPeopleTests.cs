using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationListLinkedPeopleTests
{
    [Fact]
    public void Build_marks_each_person_for_the_results_that_profile_produces()
    {
        var ali = Guid.NewGuid();
        var berdi = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
                ProduceRejection = true,
                ProduceWorkPermit = false,
                ProduceBorderZone = false,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [ali, berdi],
            new Dictionary<Guid, string>
            {
                [ali] = "Ali Aydogdy",
                [berdi] = "Berdi Annayev",
            },
            invitation: new HashSet<Guid> { ali },
            workPermit: new HashSet<Guid> { ali, berdi },
            visa: new HashSet<Guid> { berdi },
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid> { berdi });

        Assert.Equal(2, rows.Count);
        Assert.Equal("Ali Aydogdy", rows[0].Name);
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneDone),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneMissing),
            ],
            Marks(rows[0]));

        Assert.Equal("Berdi Annayev", rows[1].Name);
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneMissing),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneDone),
                (ApplicationWorkspaceIssuedRecordsCatalog.Rejection, ApplicationListLinkedPeople.ToneRejected),
            ],
            Marks(rows[1]));
    }

    [Fact]
    public void Build_is_empty_when_the_profile_has_no_active_people()
    {
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };

        Assert.Empty(ApplicationListLinkedPeople.Build(
            application,
            activeRoster: null,
            names: new Dictionary<Guid, string>(),
            invitation: new HashSet<Guid>(),
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>()));
    }

    [Fact]
    public void Build_recolors_the_issued_invitation_circle_for_later_life()
    {
        var expired = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [expired, missing],
            new Dictionary<Guid, string>
            {
                [expired] = "Meret Berdiyew",
                [missing] = "Selbi Orazowa",
            },
            invitation: new HashSet<Guid> { expired },
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid> { missing },
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>(),
            invitationLaterLife: new Dictionary<Guid, string>
            {
                [expired] = InvitationLaterLife.ExpiredUnused,
                [missing] = InvitationLaterLife.Cancelled,
            });

        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, InvitationLaterLife.ToneExpired),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneMissing),
            ],
            Marks(rows[0]));
        Assert.Equal(InvitationLaterLife.ExpiredUnused, rows[0].Marks[0].State);
        Assert.Equal(1, ApplicationListLinkedPeople.CountMarks(rows, InvitationLaterLife.ExpiredUnused));
        Assert.Equal(0, ApplicationListLinkedPeople.CountMarks(rows, InvitationLaterLife.Cancelled));
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneMissing),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, ApplicationListLinkedPeople.ToneDone),
            ],
            Marks(rows[1]));
    }

    [Fact]
    public void Invitation_later_life_is_only_one_state()
    {
        Assert.Equal(
            InvitationLaterLife.Cancelled,
            InvitationLaterLife.Resolve(cancelled: true, changed: true, used: false, expired: true));
        Assert.Equal(
            InvitationLaterLife.Changed,
            InvitationLaterLife.Resolve(cancelled: false, changed: true, used: false, expired: true));
        Assert.Equal(
            InvitationLaterLife.ExpiredUnused,
            InvitationLaterLife.Resolve(cancelled: false, changed: false, used: false, expired: true));
        Assert.Equal(
            InvitationLaterLife.Used,
            InvitationLaterLife.Resolve(cancelled: false, changed: false, used: true, expired: true));
        Assert.Equal(
            InvitationLaterLife.Used,
            InvitationLaterLife.Resolve(cancelled: false, changed: false, used: true, expired: false));
        Assert.Equal(
            InvitationLaterLife.Changed,
            InvitationLaterLife.Resolve(cancelled: false, changed: true, used: true, expired: false));
        Assert.Equal(
            InvitationLaterLife.Cancelled,
            InvitationLaterLife.Strongest(
            [
                InvitationLaterLife.Used,
                InvitationLaterLife.ExpiredUnused,
                InvitationLaterLife.Changed,
                InvitationLaterLife.Cancelled,
            ]));
    }

    [Fact]
    public void Build_keeps_not_issued_apart_from_an_expired_visa_or_work_permit()
    {
        var expired = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var current = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceWorkPermit = true,
                ProduceVisa = true,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [expired, missing, current],
            new Dictionary<Guid, string>
            {
                [current] = "Ayna Berdiyeva",
                [expired] = "Meret Annayew",
                [missing] = "Oraz Geldiyew",
            },
            invitation: new HashSet<Guid> { expired, current },
            workPermit: new HashSet<Guid> { expired, current },
            visa: new HashSet<Guid> { expired, current },
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>(),
            expiredWorkPermit: new HashSet<Guid> { expired, missing },
            expiredVisa: new HashSet<Guid> { expired, missing });

        var meret = rows.Single(row => row.PersonId == expired);
        Assert.Equal(
            [
                (ApplicationWorkspaceIssuedRecordsCatalog.Invitation, ApplicationListLinkedPeople.ToneDone),
                (ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, IssuedResultExpiry.Tone),
                (ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, IssuedResultExpiry.Tone),
            ],
            Marks(meret));
        Assert.Equal(IssuedResultExpiry.WorkPermit, meret.Marks[1].State);
        Assert.Equal(IssuedResultExpiry.Visa, meret.Marks[2].State);

        var oraz = rows.Single(row => row.PersonId == missing);
        Assert.All(oraz.Marks, mark => Assert.Equal(ApplicationListLinkedPeople.ToneMissing, mark.Tone));
        Assert.Equal(1, ApplicationListLinkedPeople.CountMarks(rows, IssuedResultExpiry.Visa));
        Assert.Equal(0, ApplicationListLinkedPeople.CountMarks(rows, InvitationLaterLife.ExpiredUnused));
    }

    [Fact]
    public void Expired_people_require_every_dated_document_to_be_past()
    {
        var instance = Guid.NewGuid();
        var person = Guid.NewGuid();
        var today = DateTime.Today;

        var mixed = IssuedResultExpiry.People(
        [
            (instance, person, today.AddDays(-2)),
            (instance, person, today.AddDays(5)),
        ]);
        Assert.False(mixed.TryGetValue(instance, out _));

        var past = IssuedResultExpiry.People(
        [
            (instance, person, today.AddDays(-2)),
            (instance, person, null),
        ]);
        Assert.Contains(person, past[instance]);

        var undated = IssuedResultExpiry.People([(instance, person, default(DateTime))]);
        Assert.False(undated.TryGetValue(instance, out _));
    }

    [Fact]
    public void Build_links_only_an_issued_circle_that_has_a_document()
    {
        var person = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProduceInvitation = true,
                ProduceVisa = true,
            },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [person],
            new Dictionary<Guid, string> { [person] = "Rohit Vilas Kakad" },
            invitation: new HashSet<Guid> { person },
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>(),
            invitationLaterLife: new Dictionary<Guid, string>
            {
                [person] = InvitationLaterLife.ExpiredUnused,
            },
            invitationDocuments: new Dictionary<Guid, Guid> { [person] = invitationId });

        var invitation = rows[0].Marks[0];
        var visa = rows[0].Marks[1];
        Assert.Equal(invitationId, invitation.DocumentId);
        Assert.Equal(InvitationLaterLife.ToneExpired, invitation.Tone);
        Assert.True(ApplicationListLinkedPeople.CanPreview(invitation));
        Assert.Equal(Guid.Empty, visa.DocumentId);
        Assert.False(ApplicationListLinkedPeople.CanPreview(visa));
    }

    [Fact]
    public void Build_places_the_new_invitation_beside_a_changed_one()
    {
        var person = Guid.NewGuid();
        var oldInvitationId = Guid.NewGuid();
        var newInvitationId = Guid.NewGuid();
        var changeApplicationId = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };
        var replacement = new ApplicationListLinkedPeople.InvitationReplacement(
            newInvitationId,
            changeApplicationId,
            "10/-2125",
            ApplicationListLinkedPeople.ToneDone,
            null);

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [person],
            new Dictionary<Guid, string> { [person] = "Weijun Liu" },
            invitation: new HashSet<Guid> { person },
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>(),
            invitationLaterLife: new Dictionary<Guid, string> { [person] = InvitationLaterLife.Changed },
            invitationDocuments: new Dictionary<Guid, Guid> { [person] = oldInvitationId },
            invitationReplacements: new Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>
            {
                [person] = replacement,
            });

        var mark = rows[0].Marks[0];
        Assert.Equal(oldInvitationId, mark.DocumentId);
        Assert.Equal(InvitationLaterLife.Changed, mark.State);
        Assert.Equal(replacement, mark.Replacement);
        Assert.Equal(InvitationLaterLife.InForce, ApplicationListLinkedPeople.InvitationLife(replacement));
    }

    [Fact]
    public void Build_does_not_add_a_new_invitation_when_the_old_one_is_still_in_force()
    {
        var person = Guid.NewGuid();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile { ProduceInvitation = true },
        };

        var rows = ApplicationListLinkedPeople.Build(
            application,
            [person],
            new Dictionary<Guid, string> { [person] = "Zihie Liu" },
            invitation: new HashSet<Guid> { person },
            workPermit: new HashSet<Guid>(),
            visa: new HashSet<Guid>(),
            borderZone: new HashSet<Guid>(),
            rejection: new HashSet<Guid>(),
            invitationReplacements: new Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>
            {
                [person] = new ApplicationListLinkedPeople.InvitationReplacement(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "10/-2125",
                    ApplicationListLinkedPeople.ToneDone,
                    null),
            });

        Assert.Null(rows[0].Marks[0].Replacement);
    }

    [Fact]
    public void ChooseSuccessor_keeps_the_latest_issued_invitation_and_its_color()
    {
        var older = Guid.NewGuid();
        var newer = Guid.NewGuid();
        var newerDocument = Guid.NewGuid();
        var chosen = ApplicationListLinkedPeople.ChooseSuccessor(
        [
            (new DateTime(2026, 1, 1), Guid.NewGuid(), older, "10/-1000", null),
            (new DateTime(2026, 10, 1), newerDocument, newer, "10/-2125", InvitationLaterLife.Used),
        ]);

        Assert.NotNull(chosen);
        Assert.Equal(newerDocument, chosen!.DocumentId);
        Assert.Equal(newer, chosen.ApplicationId);
        Assert.Equal("10/-2125", chosen.ApplicationNumber);
        Assert.Equal(InvitationLaterLife.ToneUsed, chosen.Tone);
        Assert.Equal(InvitationLaterLife.Used, chosen.State);
    }

    [Fact]
    public void Invitation_preview_keeps_every_person_even_when_the_state_matches()
    {
        var weijun = Guid.NewGuid();
        var zihie = Guid.NewGuid();
        var mixed = new[]
        {
            new InvitationPreviewStates.PersonState(weijun, "Weijun Liu", InvitationLaterLife.Changed),
            new InvitationPreviewStates.PersonState(zihie, "Zihie Liu", InvitationLaterLife.InForce),
        };
        var same = new[]
        {
            new InvitationPreviewStates.PersonState(weijun, "Weijun Liu", InvitationLaterLife.InForce),
            new InvitationPreviewStates.PersonState(zihie, "Zihie Liu", InvitationLaterLife.InForce),
        };

        Assert.Equal(2, mixed.Length);
        Assert.Equal(2, same.Length);
        Assert.Equal(InvitationLaterLife.Changed, mixed[0].Life);
        Assert.Equal(InvitationLaterLife.InForce, mixed[1].Life);
        Assert.All(same, person => Assert.Equal(InvitationLaterLife.InForce, person.Life));
    }

    [Fact]
    public void FormatName_uses_first_middle_last()
    {
        Assert.Equal("Ali Gurban Berdiyev", ApplicationListLinkedPeople.FormatName("Ali", "Gurban", "Berdiyev"));
        Assert.Equal("—", ApplicationListLinkedPeople.FormatName(" ", null, ""));
    }

    private static List<(string Key, string Tone)> Marks(ApplicationListLinkedPeople.Row row)
    {
        var marks = new List<(string Key, string Tone)>(row.Marks.Count);
        foreach (var mark in row.Marks)
            marks.Add((mark.Key, mark.Tone));
        return marks;
    }
}

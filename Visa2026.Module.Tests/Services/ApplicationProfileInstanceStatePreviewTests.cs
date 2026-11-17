using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceStatePreviewTests
{
    [Theory]
    [InlineData("get_invitation_wp", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneWorkPermitVisa)]
    [InlineData("extend_visa_wp", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneWorkPermitVisa)]
    [InlineData("get_invitation", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneVisa)]
    [InlineData("get_invitation_fm", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneVisa)]
    [InlineData("get_invitation_service_passport", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneVisa)]
    [InlineData("get_invitation_according_to_wp", ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneVisa)]
    [InlineData("workpermit_extension", ApplicationProfileInstanceStatePreview.Layout.WorkPermitLocationVisaBorderZone)]
    [InlineData("visa_ext_according_to_wp", ApplicationProfileInstanceStatePreview.Layout.WorkPermitLocationVisaBorderZone)]
    [InlineData("get_border_zone", ApplicationProfileInstanceStatePreview.Layout.BorderZone)]
    [InlineData("change_workpermit", ApplicationProfileInstanceStatePreview.Layout.WorkPermitLocations)]
    [InlineData("visa_ext", ApplicationProfileInstanceStatePreview.Layout.ProgressOnly)]
    [InlineData("seretmezlik", ApplicationProfileInstanceStatePreview.Layout.ProgressOnly)]
    [InlineData("cancel_invitation", ApplicationProfileInstanceStatePreview.Layout.ProgressOnly)]
    [InlineData(null, ApplicationProfileInstanceStatePreview.Layout.ProgressOnly)]
    public void Layout_follows_profile_code(string? code, ApplicationProfileInstanceStatePreview.Layout expected) =>
        Assert.Equal(expected, ApplicationProfileInstanceStatePreview.LayoutFor(code));

    [Fact]
    public void Full_invitation_profile_nests_border_zone_under_invitation()
    {
        var flags = new ApplicationProfileInstanceStatePreview.ProduceFlags(
            Invitation: true,
            WorkPermit: true,
            Visa: true,
            BorderZone: true,
            Location: false);
        var sections = ApplicationProfileInstanceStatePreview.SectionsFor(
            ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneWorkPermitVisa,
            flags);

        Assert.Equal(3, sections.Count);
        Assert.Equal(ApplicationProfileInstanceStatePreview.SectionId.Invitation, sections[0].Id);
        Assert.True(sections[0].NestBorderZone);
        Assert.Equal(ApplicationProfileInstanceStatePreview.SectionId.WorkPermit, sections[1].Id);
        Assert.Equal(ApplicationProfileInstanceStatePreview.SectionId.Visa, sections[2].Id);
        Assert.False(sections[2].NestBorderZone);
    }

    [Fact]
    public void Visa_and_work_permit_extension_drops_invitation_and_nests_border_zone_under_visa()
    {
        var flags = new ApplicationProfileInstanceStatePreview.ProduceFlags(
            Invitation: false,
            WorkPermit: true,
            Visa: true,
            BorderZone: true,
            Location: true);
        var sections = ApplicationProfileInstanceStatePreview.SectionsFor(
            ApplicationProfileInstanceStatePreview.Layout.InvitationBorderZoneWorkPermitVisa,
            flags);

        Assert.Equal(2, sections.Count);
        Assert.Equal(ApplicationProfileInstanceStatePreview.SectionId.WorkPermit, sections[0].Id);
        Assert.True(sections[0].NestLocations);
        Assert.Equal(ApplicationProfileInstanceStatePreview.SectionId.Visa, sections[1].Id);
        Assert.True(sections[1].NestBorderZone);
    }

    [Fact]
    public void Progress_only_layout_has_no_document_sections()
    {
        var flags = new ApplicationProfileInstanceStatePreview.ProduceFlags(true, true, true, true, true);
        var sections = ApplicationProfileInstanceStatePreview.SectionsFor(
            ApplicationProfileInstanceStatePreview.Layout.ProgressOnly,
            flags);
        Assert.Empty(sections);
    }

    [Fact]
    public void Issued_document_number_can_open_its_copy_and_a_waiting_row_cannot()
    {
        var issued = new ApplicationProfileInstanceStatePreview.Row(
            "A",
            "1574",
            "01.01.2026",
            "k",
            "inforce",
            Nested: false,
            PreviewParentId: Guid.NewGuid(),
            PreviewFamily: HeaderDocumentCopiesFamily.Invitation,
            InvitationLifeKey: "invitationInForce");
        var waiting = new ApplicationProfileInstanceStatePreview.Row(
            "A",
            "—",
            "—",
            "k",
            "waiting",
            Nested: false);

        Assert.True(issued.CanPreview);
        Assert.Equal(HeaderDocumentCopiesFamily.Invitation, issued.PreviewFamily);
        Assert.False(waiting.CanPreview);
    }

    [Fact]
    public void Invitation_people_share_one_number_and_not_issued_names_follow()
    {
        var header = Guid.NewGuid();
        var zone = Guid.NewGuid();
        var rows = new[]
        {
            Person("Harish Yamala", "CO338630", "27.10.2026 – 26.01.2027", header, "invitationInForce"),
            Zone("Balkanabat", zone),
            Person("Prathap Gajjela", "CO338630", "27.10.2026 – 26.01.2027", header, "invitationInForce"),
            Waiting("Ryotaro Oki"),
        };

        var grouped = ApplicationProfileInstanceStatePreview.GroupHeaders(rows, periodOnHeader: true);

        Assert.Single(grouped.Groups);
        var group = grouped.Groups[0];
        Assert.Equal("CO338630", group.Number);
        Assert.Equal("27.10.2026 – 26.01.2027", group.Period);
        Assert.True(group.PeriodOnHeader);
        Assert.Equal(header, group.PreviewParentId);
        Assert.Equal("invitationInForce", group.InvitationLifeKey);
        Assert.Equal(Guid.Empty, group.PreviewPersonId);
        Assert.Equal(new[] { "Harish Yamala", "Prathap Gajjela" }, group.People.Select(person => person.PersonName));
        Assert.Single(group.People[0].Nested);
        Assert.Equal(new[] { "Ryotaro Oki" }, grouped.NotIssuedNames);
    }

    [Fact]
    public void Sheet_keeps_the_visa_issue_window_for_the_waiting_heading()
    {
        var counter = new VisaIssueWindowCalculator.Badge(32, "ok", false, "title");
        var wait = new VisaIssuePassportWait(counter, new[] { "Caner Faki" });
        var sheet = new ApplicationProfileInstanceStatePreview.Sheet(
            Number: "8/-1",
            ProfileName: "extend",
            ProcessNumber: "",
            Date: "01.01.2026",
            VisaType: "",
            VisaPeriod: "",
            Project: "",
            Urgency: "",
            PersonCount: 1,
            CompletionPercent: 0,
            Steps: Array.Empty<ApplicationWorkspaceListProgressSteps.Step>(),
            Seretmezlik: Array.Empty<ApplicationProfileInstanceStatePreview.SeretmezlikLine>(),
            Sections: Array.Empty<ApplicationProfileInstanceStatePreview.Section>(),
            VisaPassportWait: wait);

        Assert.NotNull(sheet.VisaPassportWait);
        Assert.Equal(32, sheet.VisaPassportWait!.Counter.Days);
        Assert.Equal("ok", sheet.VisaPassportWait.Counter.Tone);
        Assert.Equal(new[] { "Caner Faki" }, sheet.VisaPassportWait.Names);
    }

    [Fact]
    public void Issued_visas_are_listed_apart_from_visas_not_issued_yet()
    {
        var visa = Guid.NewGuid();
        var zone = Guid.NewGuid();
        var rows = new[]
        {
            Waiting("Caner Faki"),
            Person("Coşkun Demirbilek", "A1758468", "29.12.2026 – 28.06.2027", visa, null, HeaderDocumentCopiesFamily.Visa),
            Zone("Ahal", zone),
            Waiting("Fatih Akgöllü"),
        };

        var split = ApplicationProfileInstanceStatePreview.SplitNotIssued(rows);

        Assert.Equal(new[] { "Coşkun Demirbilek", "" }, split.Issued.Select(row => row.PersonName));
        Assert.True(split.Issued[1].Nested);
        Assert.Equal(new[] { "Caner Faki", "Fatih Akgöllü" }, split.NotIssuedNames);
    }

    [Fact]
    public void Border_zone_without_a_visa_stays_on_the_visa_table()
    {
        var rows = new[]
        {
            Waiting("Caner Faki"),
            Zone("Ahal", Guid.NewGuid()),
        };

        var split = ApplicationProfileInstanceStatePreview.SplitNotIssued(rows);

        Assert.Equal(2, split.Issued.Count);
        Assert.Empty(split.NotIssuedNames);
    }

    [Fact]
    public void Seretmezlik_letter_names_the_submit_to_organization()
    {
        Assert.Equal(
            "8/-33 · Migrasiýa",
            ApplicationProfileInstanceStatePreview.SeretmezlikLetterText("8/-33", "Migrasiýa"));
        Assert.Equal("8/-33", ApplicationProfileInstanceStatePreview.SeretmezlikLetterText("8/-33", "  "));
        Assert.Equal(
            "Migrasiýa",
            ApplicationProfileInstanceStatePreview.DestinationLabel(" Migrasiýa ", "Daşary işler ministrligi", "Migrasiýa gullugy"));
        Assert.Equal(
            "Daşary işler ministrligi",
            ApplicationProfileInstanceStatePreview.DestinationLabel(null, "Daşary işler ministrligi", "Migrasiýa gullugy"));
        Assert.Equal(
            "Migrasiýa gullugy",
            ApplicationProfileInstanceStatePreview.DestinationLabel(null, " ", "Migrasiýa gullugy"));
        Assert.Null(ApplicationProfileInstanceStatePreview.DestinationLabel(null, null, null));
    }

    [Fact]
    public void Seretmezlik_people_are_not_listed_as_not_issued()
    {
        var excluded = Guid.NewGuid();
        var waiting = Guid.NewGuid();
        var ids = new HashSet<Guid> { excluded };

        Assert.False(ApplicationProfileInstanceStatePreview.ShowAsNotIssued(excluded, ids));
        Assert.True(ApplicationProfileInstanceStatePreview.ShowAsNotIssued(waiting, ids));
        Assert.True(ApplicationProfileInstanceStatePreview.ShowAsNotIssued(excluded, null));
    }

    [Fact]
    public void Work_permits_keep_one_number_and_each_person_period()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var rows = new[]
        {
            Person("B Person", "WP-2", "01.02.2026 – 01.05.2026", second, null, HeaderDocumentCopiesFamily.WorkPermit),
            Person("A Person", "WP-1", "01.01.2026 – 01.04.2026", first, null, HeaderDocumentCopiesFamily.WorkPermit),
            Person("C Person", "WP-1", "01.03.2026 – 01.06.2026", first, null, HeaderDocumentCopiesFamily.WorkPermit),
        };

        var grouped = ApplicationProfileInstanceStatePreview.GroupHeaders(rows, periodOnHeader: false);

        Assert.Equal(new[] { "WP-1", "WP-2" }, grouped.Groups.Select(group => group.Number));
        Assert.False(grouped.Groups[0].PeriodOnHeader);
        Assert.Equal(new[] { "A Person", "C Person" }, grouped.Groups[0].People.Select(person => person.PersonName));
        Assert.Equal("01.01.2026 – 01.04.2026", grouped.Groups[0].People[0].Period);
        Assert.Equal("01.03.2026 – 01.06.2026", grouped.Groups[0].People[1].Period);
        Assert.Empty(grouped.NotIssuedNames);
    }

    [Theory]
    [InlineData(null, "ApplicationProfileInstance.List.Mark.InvitationInForce", "inforce")]
    [InlineData("invitationInForce", "ApplicationProfileInstance.List.Mark.InvitationInForce", "inforce")]
    [InlineData("invitationUsed", "ApplicationProfileInstance.List.Mark.InvitationUsedTitle", "used")]
    [InlineData("invitationExpiredUnused", "ApplicationProfileInstance.List.Mark.DocumentExpiredTitle", "expired")]
    [InlineData("invitationChanged", "ApplicationProfileInstance.List.Mark.InvitationChangedTitle", "changed")]
    [InlineData("invitationCancelled", "ApplicationProfileInstance.List.Mark.InvitationCancelledTitle", "cancelled")]
    public void Document_life_names_invitation_and_work_permit_states(string? life, string key, string tone)
    {
        var label = ApplicationProfileInstanceStatePreview.DocumentLife(life);
        Assert.Equal(key, label.Key);
        Assert.Equal(tone, label.Tone);
    }

    [Fact]
    public void Shared_work_permit_state_stays_on_the_group()
    {
        var header = Guid.NewGuid();
        var rows = new[]
        {
            Person("A Person", "WP-1", "01.01.2026 – 01.04.2026", header, "invitationExpiredUnused", HeaderDocumentCopiesFamily.WorkPermit),
            Person("B Person", "WP-1", "01.02.2026 – 01.05.2026", header, "invitationExpiredUnused", HeaderDocumentCopiesFamily.WorkPermit),
        };

        var grouped = ApplicationProfileInstanceStatePreview.GroupHeaders(rows, periodOnHeader: false);

        Assert.Equal("invitationExpiredUnused", grouped.Groups[0].InvitationLifeKey);
        Assert.Equal("expired", ApplicationProfileInstanceStatePreview.DocumentLife(grouped.Groups[0].InvitationLifeKey).Tone);
    }

    [Fact]
    public void Mixed_invitation_states_do_not_label_the_whole_group()
    {
        var header = Guid.NewGuid();
        var rows = new[]
        {
            Person("A", "1574", "01.01.2026", header, "invitationInForce"),
            Person("B", "1574", "01.01.2026", header, "invitationUsed"),
        };

        var grouped = ApplicationProfileInstanceStatePreview.GroupHeaders(rows, periodOnHeader: true);

        Assert.Null(grouped.Groups[0].InvitationLifeKey);
    }

    [Fact]
    public void Changed_invitation_keeps_the_new_invitation_on_the_person()
    {
        var header = Guid.NewGuid();
        var successor = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var row = new ApplicationProfileInstanceStatePreview.Row(
            "A",
            "1574",
            "01.01.2026",
            "ApplicationProfileInstance.List.Mark.InvitationChangedTitle",
            "changed",
            Nested: false,
            PreviewParentId: header,
            PreviewFamily: HeaderDocumentCopiesFamily.Invitation,
            PreviewPersonId: personId,
            InvitationLifeKey: "invitationChanged",
            SuccessorNumber: "9/-100",
            SuccessorDocumentId: successor,
            SuccessorLifeKey: "invitationInForce");

        var grouped = ApplicationProfileInstanceStatePreview.GroupHeaders(new[] { row }, periodOnHeader: true);
        var person = grouped.Groups[0].People[0];

        Assert.Equal("9/-100", person.SuccessorNumber);
        Assert.Equal(successor, person.SuccessorDocumentId);
        Assert.Equal("invitationInForce", person.SuccessorLifeKey);
        Assert.Equal(personId, person.SuccessorPersonId);
    }

    private static ApplicationProfileInstanceStatePreview.Row Person(
        string name,
        string number,
        string period,
        Guid header,
        string? life,
        HeaderDocumentCopiesFamily family = HeaderDocumentCopiesFamily.Invitation) =>
        new(name, number, period, "k", "inforce", false, header, family, Guid.NewGuid(), life);

    private static ApplicationProfileInstanceStatePreview.Row Waiting(string name) =>
        new(name, "—", "—", "ApplicationProfileInstance.List.Mark.NotIssuedTitle", "waiting", false);

    private static ApplicationProfileInstanceStatePreview.Row Zone(string place, Guid header) =>
        new(string.Empty, place, "01.01.2026 – 01.06.2026", "k", "inforce", true, header, HeaderDocumentCopiesFamily.BorderZone, Guid.NewGuid(), null);
}

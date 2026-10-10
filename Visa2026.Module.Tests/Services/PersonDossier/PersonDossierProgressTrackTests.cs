using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PersonDossier;
using Xunit;

namespace Visa2026.Module.Tests.Services.PersonDossier;

public class PersonDossierProgressTrackTests
{
    [Fact]
    public void ToDossierProgressSteps_uses_list_tone_glyph_and_done_connector()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 10, 6),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        var steps = PersonDossierResolver.ToDossierProgressSteps(
            ApplicationWorkspaceListProgressSteps.BuildTimeline(application, []));

        Assert.Equal(2, steps.Count);
        Assert.Equal("current", steps[0].Tone);
        Assert.Equal("1", steps[0].Glyph);
        Assert.False(steps[0].ConnectorDone);
        Assert.False(string.IsNullOrWhiteSpace(steps[0].StatusLabel));
        Assert.Equal("pending", steps[1].Tone);
        Assert.Equal("2", steps[1].Glyph);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].StatusLabel));
        Assert.True(steps[0].IsOfficeFile);
        Assert.False(steps[0].ShowLetter);
        Assert.False(steps[0].ShowMissingLetter);
    }

    [Fact]
    public void Finished_office_step_links_the_uploaded_file()
    {
        var steps = OfficeSteps(withFile: true);

        Assert.True(steps[0].IsOfficeFile);
        Assert.False(steps[0].IsCurrentStep);
        Assert.True(steps[0].ShowLetter);
        Assert.False(steps[0].ShowMissingLetter);
        Assert.Equal("office-scan.pdf", steps[0].LetterFileName);
    }

    [Fact]
    public void Issued_column_waits_until_migration_is_finished()
    {
        var invitation = InvitationFor("C0021450", new DateTime(2026, 10, 6));
        var pending = new[]
        {
            new ApplicationWorkspaceCaseProgressStep { Key = "migration", State = "current", OutcomeKind = "current" },
        };

        Assert.Null(PersonDossierResolver.ResolveIssuedOutcome(pending, invitation, rejectionItem: null));
    }

    [Fact]
    public void Finished_migration_shows_this_persons_invitation()
    {
        var invitation = InvitationFor("C0021450", new DateTime(2026, 12, 1));
        var done = new[]
        {
            new ApplicationWorkspaceCaseProgressStep { Key = "migration", State = "done", OutcomeKind = "issued" },
        };

        var outcome = PersonDossierResolver.ResolveIssuedOutcome(done, invitation, rejectionItem: null);

        Assert.NotNull(outcome);
        Assert.Equal("C0021450", outcome!.Number);
        Assert.False(string.IsNullOrWhiteSpace(outcome.StatusLabel));
    }

    [Fact]
    public void Rejected_migration_shows_the_rejection_instead_of_the_invitation()
    {
        var invitation = InvitationFor("C0021450", new DateTime(2026, 12, 1));
        var rejection = new RejectionItem
        {
            Rejection = new Rejection { RejectedDocNumber = "RJ-104", Date = new DateTime(2026, 8, 12) },
        };
        var done = new[]
        {
            new ApplicationWorkspaceCaseProgressStep { Key = "migration", State = "done", OutcomeKind = "rejected" },
        };

        var outcome = PersonDossierResolver.ResolveIssuedOutcome(done, invitation, rejection);

        Assert.NotNull(outcome);
        Assert.Equal("RJ-104", outcome!.Number);
        Assert.Equal(VisaUiMessages.Get("PersonDossier.Status.Rejected"), outcome.StatusLabel);
    }

    [Fact]
    public void Finished_office_step_without_a_file_is_missing()
    {
        var steps = OfficeSteps(withFile: false);

        Assert.False(steps[0].IsCurrentStep);
        Assert.False(steps[0].ShowLetter);
        Assert.True(steps[0].ShowMissingLetter);
    }

    [Fact]
    public void Paper_track_replaces_status_column_when_person_is_not_excluded()
    {
        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(Snapshot(excluded: false), "en-US");

        Assert.Contains("Application progress", html, StringComparison.Ordinal);
        Assert.Contains("Office", html, StringComparison.Ordinal);
        Assert.Contains("06.10.2026", html, StringComparison.Ordinal);
        Assert.Contains("\u2713", html, StringComparison.Ordinal);
        Assert.Contains("Result number: 4/3922", html, StringComparison.Ordinal);
        Assert.Contains("approval.pdf", html, StringComparison.Ordinal);
        Assert.Contains("Missing", html, StringComparison.Ordinal);
        Assert.DoesNotContain(">Status<", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Paper_track_keeps_seretmezlik_badge_beside_the_stepper()
    {
        var html = PersonDossierDocumentHtmlBuilder.BuildFragment(Snapshot(excluded: true), "en-US");

        Assert.Contains("Application progress", html, StringComparison.Ordinal);
        Assert.Contains(">Status<", html, StringComparison.Ordinal);
        Assert.Contains("Seretmezlik", html, StringComparison.Ordinal);
        Assert.Contains("\u2713", html, StringComparison.Ordinal);
    }

    private static InvitationItem InvitationFor(string number, DateTime expiration) =>
        new()
        {
            Invitation = new Invitation
            {
                InvitationNumber = number,
                ExpirationDate = expiration,
            },
        };

    private static IReadOnlyList<PersonDossierProgressStep> OfficeSteps(bool withFile)
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 4, 5),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var row = new ApplicationProfileInstanceProgress
        {
            ID = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2026, 4, 5),
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
        };
        if (withFile)
            row.MinistryLetterFile = new FileData { FileName = "office-scan.pdf" };
        application.ProgressHistory.Add(row);

        return PersonDossierResolver.ToDossierProgressSteps(
            ApplicationWorkspaceListProgressSteps.BuildTimeline(application, application.ProgressHistory.ToList()));
    }

    private static PersonDossierSnapshot Snapshot(bool excluded) => new()
    {
        PersonId = Guid.NewGuid(),
        PersonDisplayName = "Ferdi Tiryaki",
        Sections =
        [
            new PersonDossierSection
            {
                SectionId = "applications",
                SectionLabel = "Applications",
                HasProgressColumn = true,
                ColumnHeaders = ["Application #", "Application profile", "Application date"],
                Records =
                [
                    new PersonDossierRecord
                    {
                        Cells = ["W-1001", "Calik Almak", "30 Sep 2026"],
                        StatusLabel = excluded ? "Seretmezlik · № 14 · 12.01.2026" : string.Empty,
                        StatusCssClass = excluded ? "st-expiring" : string.Empty,
                        ProgressSteps =
                        [
                            new PersonDossierProgressStep
                            {
                                Label = "Office",
                                Date = "06.10.2026",
                                StatusLabel = "Submitted",
                                Tone = "done",
                                Glyph = "\u2713",
                                ConnectorDone = true,
                                ResultNumber = "4/3922",
                                LetterFileName = "approval.pdf",
                                LetterProgressId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                            },
                            new PersonDossierProgressStep
                            {
                                Label = "Migration",
                                StatusLabel = "Current",
                                Tone = "current",
                                Glyph = "2",
                                MissingLetter = true,
                            },
                        ],
                    },
                ],
            },
        ],
    };
}

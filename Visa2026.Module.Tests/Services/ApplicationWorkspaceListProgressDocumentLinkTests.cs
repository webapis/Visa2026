using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Covers FromTimeline mapping of step document numbers and the Faýly görmek link
/// fields (DocumentProgressId / HasDocuments / AsNumberReceived) added for list rows.
/// </summary>
public sealed class ApplicationWorkspaceListProgressDocumentLinkTests
{
    [Fact]
    public void FromTimeline_appends_result_and_submission_numbers_under_the_step()
    {
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = "leg-1",
                Label = "Ministry",
                State = "done",
                OutcomeKind = "ok",
                CurrentStateLabel = string.Empty,
                ResultNumber = "4/4114",
                SubmissionDocumentNumber = "SUB-9",
            },
        ]);

        Assert.Single(steps);
        Assert.Contains("4/4114", steps[0].StatusLabel, StringComparison.Ordinal);
        Assert.Contains("SUB-9", steps[0].StatusLabel, StringComparison.Ordinal);
        Assert.Contains(" /", steps[0].StatusLabel, StringComparison.Ordinal);
        Assert.False(steps[0].HasDocuments);
        Assert.Equal(Guid.Empty, steps[0].DocumentProgressId);
        Assert.False(steps[0].AsNumberReceived);
    }

    [Fact]
    public void FromTimeline_migration_process_number_alone_sets_AsNumberReceived()
    {
        var progressId = Guid.NewGuid();
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
                Label = "Migration",
                State = "current",
                OutcomeKind = "current",
                CurrentStateLabel = string.Empty,
                ProcessNumber = "  CO0236909  ",
                ProgressId = progressId,
                MinistryLetterFileName = "letter.pdf",
            },
        ]);

        Assert.Single(steps);
        Assert.Equal("CO0236909", steps[0].StatusLabel);
        Assert.True(steps[0].AsNumberReceived);
        Assert.Equal(progressId, steps[0].DocumentProgressId);
        Assert.True(steps[0].HasDocuments);
    }

    [Fact]
    public void FromTimeline_issued_migration_without_process_number_still_marks_AsNumberReceived()
    {
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
                Label = "Migration",
                State = "done",
                OutcomeKind = "issued",
                CurrentStateLabel = string.Empty,
            },
        ]);

        Assert.True(steps[0].AsNumberReceived);
        Assert.False(steps[0].HasDocuments);
    }

    [Fact]
    public void FromTimeline_does_not_duplicate_a_number_already_in_the_status()
    {
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationWorkspaceProgressTimeline.MigrationKey,
                Label = "Migration",
                State = "current",
                OutcomeKind = "current",
                CurrentStateLabel = string.Empty,
                ProcessNumber = "CO1",
                ResultNumber = "CO1",
            },
        ]);

        Assert.Equal("CO1", steps[0].StatusLabel);
    }

    [Fact]
    public void FromTimeline_files_without_progress_id_do_not_enable_HasDocuments()
    {
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = "leg-1",
                Label = "Ministry",
                State = "done",
                OutcomeKind = "ok",
                Files =
                [
                    new ApplicationProfileInstanceProgressStoredFile(Guid.NewGuid(), "scan.pdf"),
                ],
            },
        ]);

        Assert.False(steps[0].HasDocuments);
        Assert.Equal(Guid.Empty, steps[0].DocumentProgressId);
    }

    [Fact]
    public void FromTimeline_uses_DateProgressId_when_ProgressId_is_missing()
    {
        var dateId = Guid.NewGuid();
        var steps = ApplicationWorkspaceListProgressSteps.FromTimeline(
        [
            new ApplicationWorkspaceCaseProgressStep
            {
                Key = ApplicationMigrationSubmission.TimelineKey,
                Label = "Hand-in",
                State = "done",
                OutcomeKind = "ok",
                DateProgressId = dateId,
                SubmissionDocumentNumber = "HN-22",
                Files =
                [
                    new ApplicationProfileInstanceProgressStoredFile(Guid.NewGuid(), "handin.pdf"),
                ],
            },
        ]);

        Assert.Equal(dateId, steps[0].DocumentProgressId);
        Assert.True(steps[0].HasDocuments);
        Assert.Equal("HN-22", steps[0].StatusLabel);
        Assert.False(steps[0].AsNumberReceived);
    }
}

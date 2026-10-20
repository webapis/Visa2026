using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Guards for the via-ministry "papers handed to migration" step:
/// document number validation and completeness before PROCESS_STARTED.
/// </summary>
public class ApplicationMigrationSubmissionTests
{
    [Theory]
    [InlineData("SUBMITTED_MIGRATION", true)]
    [InlineData("submitted_migration", true)]
    [InlineData(" SUBMITTED_MIGRATION ", true)]
    [InlineData("PROCESS_STARTED", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsState_MatchesSubmittedToMigrationOnly(string? code, bool expected) =>
        Assert.Equal(expected, ApplicationMigrationSubmission.IsState(code));

    [Fact]
    public void TryNormalizeDocumentNumber_RejectsBlank()
    {
        var ok = ApplicationMigrationSubmission.TryNormalizeDocumentNumber("  ", out var normalized, out var error);

        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberRequired"),
            error);
    }

    [Fact]
    public void TryNormalizeDocumentNumber_TrimsValidValue()
    {
        Assert.True(ApplicationMigrationSubmission.TryNormalizeDocumentNumber(
            "  CO-12/2026  ",
            out var normalized,
            out var error));
        Assert.Equal("CO-12/2026", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalizeDocumentNumber_RejectsOverMaxLength()
    {
        var ok = ApplicationMigrationSubmission.TryNormalizeDocumentNumber(
            new string('A', ApplicationMigrationSubmission.MaxDocumentNumberLength + 1),
            out var normalized,
            out var error);

        Assert.False(ok);
        Assert.Equal(string.Empty, normalized);
        Assert.Equal(
            VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberTooLong"),
            error);
    }

    [Fact]
    public void HasDocumentCopy_RequiresNonEmptyFile()
    {
        Assert.False(ApplicationMigrationSubmission.HasDocumentCopy(null));
        Assert.False(ApplicationMigrationSubmission.HasDocumentCopy(new ApplicationProfileInstanceProgress()));
        Assert.False(ApplicationMigrationSubmission.HasDocumentCopy(new ApplicationProfileInstanceProgress
        {
            SubmissionDocumentFile = new FileData { FileName = "empty.pdf", Size = 0, Content = [] },
        }));
        Assert.True(ApplicationMigrationSubmission.HasDocumentCopy(new ApplicationProfileInstanceProgress
        {
            SubmissionDocumentFile = new FileData { FileName = "scan.pdf", Size = 4, Content = [1, 2, 3, 4] },
        }));
    }

    [Fact]
    public void IsComplete_RequiresStateDateNumberAndCopy()
    {
        var incomplete = new ApplicationProfileInstanceProgress
        {
            Date = DateTime.Today,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration },
            SubmissionDocumentNumber = "CO-1",
        };
        Assert.False(ApplicationMigrationSubmission.IsComplete(incomplete));

        incomplete.SubmissionDocumentFile = new FileData { FileName = "scan.pdf", Size = 2, Content = [9, 9] };
        Assert.True(ApplicationMigrationSubmission.IsComplete(incomplete));

        Assert.False(ApplicationMigrationSubmission.IsComplete(new ApplicationProfileInstanceProgress
        {
            Date = default,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration },
            SubmissionDocumentNumber = "CO-1",
            SubmissionDocumentFile = new FileData { FileName = "scan.pdf", Size = 2, Content = [9, 9] },
        }));

        Assert.False(ApplicationMigrationSubmission.IsComplete(new ApplicationProfileInstanceProgress
        {
            Date = DateTime.Today,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessStarted },
            SubmissionDocumentNumber = "CO-1",
            SubmissionDocumentFile = new FileData { FileName = "scan.pdf", Size = 2, Content = [9, 9] },
        }));
    }
}

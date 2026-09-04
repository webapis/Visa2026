using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceProgressLetterCompletenessTests
{
    [Fact]
    public void ResolveMissing_decision_without_file_is_missing()
    {
        Assert.True(ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing("leg-1", isMinistryDecision: true, hasLetter: false));
    }

    [Fact]
    public void ResolveMissing_decision_with_file_is_not_missing()
    {
        Assert.False(ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing("leg-1", isMinistryDecision: true, hasLetter: true));
    }

    [Fact]
    public void ResolveMissing_office_and_submitted_are_not_missing()
    {
        Assert.False(ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing("office", isMinistryDecision: true, hasLetter: false));
        Assert.False(ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing("migration", isMinistryDecision: true, hasLetter: false));
        Assert.False(ApplicationWorkspaceProgressLetterCompleteness.ResolveMissing("leg-1", isMinistryDecision: false, hasLetter: false));
    }

    [Fact]
    public void MissingCount_counts_completed_decisions_without_letters()
    {
        var view = new ApplicationWorkspaceCaseView
        {
            ProgressSteps =
            [
                new() { Key = "office", MissingMinistryLetter = false },
                new() { Key = "leg-1", MissingMinistryLetter = true },
                new() { Key = "leg-2", MissingMinistryLetter = true },
                new() { Key = "migration", MissingMinistryLetter = false },
            ],
        };

        Assert.Equal(2, ApplicationWorkspaceProgressLetterCompleteness.MissingCount(view));
        Assert.True(ApplicationWorkspaceProgressLetterCompleteness.IsMissingLetter(view.ProgressSteps[1]));
    }
}
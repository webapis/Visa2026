using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceCaseSummaryCompletenessGateTests
{
    [Fact]
    public void MissingRequiredFields_ignores_process_number_and_filled_tiles()
    {
        var fields = new[]
        {
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType, "Visa type", ApplicationWorkspaceCaseSummaryFillState.Default),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, "Process number", ApplicationWorkspaceCaseSummaryFillState.Empty),
        };

        var missing = ApplicationWorkspaceCaseSummaryCompletenessGate.MissingRequiredFields(fields);

        var field = Assert.Single(missing);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, field.Key);
    }

    [Fact]
    public void BlocksTab_office_prep_blocks_progress_not_people()
    {
        var view = OfficeView(
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.WorkPermitLocation, "Work permit location", ApplicationWorkspaceCaseSummaryFillState.Empty));

        Assert.True(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksProcessNavigation(view));
        Assert.True(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "progress"));
        Assert.True(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "documents"));
        Assert.True(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "resminamalar"));
        Assert.True(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "sla"));
        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "people"));
        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "overview"));
    }

    [Fact]
    public void BlocksTab_after_office_prep_does_not_lock()
    {
        var view = new ApplicationWorkspaceCaseView
        {
            ProgressSteps =
            [
                new ApplicationWorkspaceCaseProgressStep { Key = "office", State = "done" },
                new ApplicationWorkspaceCaseProgressStep { Key = "leg-1", State = "current" },
            ],
            HeaderFields =
            [
                Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Empty),
            ],
        };

        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.IsOfficePreparation(view));
        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "progress"));
    }

    [Fact]
    public void BlocksTab_blue_defaults_are_complete()
    {
        var view = OfficeView(
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType, "Visa type", ApplicationWorkspaceCaseSummaryFillState.Default),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Officer));

        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksProcessNavigation(view));
        Assert.False(ApplicationWorkspaceCaseSummaryCompletenessGate.BlocksTab(view, "progress"));
    }

    [Fact]
    public void FormatBannerMessage_lists_missing_labels()
    {
        var missing = new[]
        {
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.WorkPermitLocation, "Work permit location", ApplicationWorkspaceCaseSummaryFillState.Empty),
        };

        var message = ApplicationWorkspaceCaseSummaryCompletenessGate.FormatBannerMessage(missing);

        Assert.Contains("Project, Work permit location", message);
        Assert.Contains("fields", message);
        Assert.Equal(" (2 missing)", ApplicationWorkspaceCaseSummaryCompletenessGate.FormatReadinessMissingSuffix(missing));
    }

    private static ApplicationWorkspaceCaseView OfficeView(params ApplicationWorkspaceCaseHeaderField[] fields) =>
        new()
        {
            ProgressSteps =
            [
                new ApplicationWorkspaceCaseProgressStep { Key = "office", State = "current" },
            ],
            HeaderFields = fields,
        };

    private static ApplicationWorkspaceCaseHeaderField Field(
        string key,
        string label,
        ApplicationWorkspaceCaseSummaryFillState fill) =>
        new()
        {
            Key = key,
            Label = label,
            FillState = fill,
        };
}
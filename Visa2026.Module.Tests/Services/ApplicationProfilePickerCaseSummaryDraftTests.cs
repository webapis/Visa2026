using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerCaseSummaryDraftTests
{
    [Fact]
    public void ForCreate_hides_auto_number_date_and_process_number()
    {
        var fields = ApplicationProfilePickerCaseSummaryDraft.ForCreate(
        [
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Officer),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber, "Application number", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate, "Application date", ApplicationWorkspaceCaseSummaryFillState.Default),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, "Process number", ApplicationWorkspaceCaseSummaryFillState.Empty),
        ]);

        var field = Assert.Single(fields);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, field.Key);
    }

    [Fact]
    public void CanCreate_false_when_required_field_empty()
    {
        var fields = new[]
        {
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType, "Visa type", ApplicationWorkspaceCaseSummaryFillState.Default),
        };

        Assert.False(ApplicationProfilePickerCaseSummaryDraft.CanCreate(fields));
    }

    [Fact]
    public void CanCreate_true_when_defaults_fill_required_fields()
    {
        var fields = new[]
        {
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType, "Visa type", ApplicationWorkspaceCaseSummaryFillState.Default),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Project", ApplicationWorkspaceCaseSummaryFillState.Officer),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, "Process number", ApplicationWorkspaceCaseSummaryFillState.Empty),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber, "Application number", ApplicationWorkspaceCaseSummaryFillState.Empty),
        };

        Assert.True(ApplicationProfilePickerCaseSummaryDraft.CanCreate(fields));
    }

    [Fact]
    public void Merge_last_write_wins_per_key()
    {
        var first = new ApplicationWorkspaceCaseHeaderFieldUpdate
        {
            Key = ApplicationWorkspaceCaseHeaderFieldsHelper.Project,
            Value = "old",
        };
        var second = new ApplicationWorkspaceCaseHeaderFieldUpdate
        {
            Key = ApplicationWorkspaceCaseHeaderFieldsHelper.Project,
            Value = "new",
        };

        var merged = ApplicationProfilePickerCaseSummaryDraft.Merge(
            new[] { first },
            second);

        var update = Assert.Single(merged);
        Assert.Equal("new", update.Value);
    }

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
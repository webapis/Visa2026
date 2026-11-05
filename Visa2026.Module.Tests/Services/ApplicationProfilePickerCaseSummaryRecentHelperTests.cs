using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerCaseSummaryRecentHelperTests
{
    [Fact]
    public void Push_dedupes_same_updates_and_caps()
    {
        var first = Entry("A", "p1");
        var second = Entry("B", "p1"); // same project update value → same fingerprint
        var third = Entry("C", "p2");

        var list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(null, first, cap: 2);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, second, cap: 2);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, third, cap: 2);

        Assert.Equal(2, list.Count);
        Assert.Equal(third.Id, list[0].Id);
        Assert.Equal(second.Id, list[1].Id);
    }

    [Fact]
    public void Serialize_round_trips_updates()
    {
        var entry = Entry("X", "abc");
        var json = ApplicationProfilePickerCaseSummaryRecentHelper.Serialize(new[] { entry });
        var loaded = ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(json);
        var one = Assert.Single(loaded);
        Assert.Equal(entry.Id, one.Id);
        Assert.Equal("abc", Assert.Single(one.Updates).Value);
        Assert.Equal("Proj X", one.ProjectLabel);
    }

    [Fact]
    public void CaptureUpdates_skips_hidden_create_fields()
    {
        var updates = ApplicationProfilePickerCaseSummaryRecentHelper.CaptureUpdates(
        [
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, Guid.Parse("11111111-1111-1111-1111-111111111111"), "P"),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber, null, "1"),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, null, "2"),
        ]);

        var one = Assert.Single(updates);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, one.Key);
        Assert.Equal("11111111-1111-1111-1111-111111111111", one.Value);
    }

    [Fact]
    public void FormatRelativeTime_just_now_and_hours()
    {
        var now = new DateTime(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);
        var justNow = ApplicationProfilePickerCaseSummaryRecentHelper.FormatRelativeTime(now.AddSeconds(-20), now);
        Assert.False(string.IsNullOrWhiteSpace(justNow));
        var hours = ApplicationProfilePickerCaseSummaryRecentHelper.FormatRelativeTime(now.AddHours(-3), now);
        Assert.Contains("3", hours, StringComparison.Ordinal);
    }

    private static ApplicationProfilePickerCaseSummaryRecentEntry Entry(string project, string projectId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UsedAtUtc = DateTime.UtcNow,
            ProjectLabel = "Proj " + project,
            CompanyLabel = "Co",
            VisaTypeLabel = "Work",
            PeriodLabel = "30 days",
            Updates =
            [
                new ApplicationWorkspaceCaseHeaderFieldUpdate
                {
                    Key = ApplicationWorkspaceCaseHeaderFieldsHelper.Project,
                    Value = projectId,
                },
            ],
        };

    private static ApplicationWorkspaceCaseHeaderField Field(string key, Guid? selectedId, string value) =>
        new()
        {
            Key = key,
            SelectedId = selectedId,
            Value = value,
            DisplayValue = value,
            Kind = selectedId.HasValue
                ? ApplicationWorkspaceCaseHeaderFieldKind.Lookup
                : ApplicationWorkspaceCaseHeaderFieldKind.ShortText,
        };
}
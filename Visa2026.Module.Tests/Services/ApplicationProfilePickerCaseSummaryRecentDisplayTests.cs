using System;
using System.Collections.Generic;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerCaseSummaryRecentDisplayTests
{
    [Fact]
    public void StorageKey_uses_anon_for_blank_and_trims_user()
    {
        Assert.Equal(
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKeyPrefix + "anon",
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey(null));
        Assert.Equal(
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKeyPrefix + "anon",
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey("  "));
        Assert.Equal(
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKeyPrefix + "officer.a",
            ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey(" officer.a "));
    }

    [Fact]
    public void PrimaryLabel_prefers_project_then_visa_type_then_untitled()
    {
        Assert.False(string.IsNullOrWhiteSpace(
            ApplicationProfilePickerCaseSummaryRecentHelper.PrimaryLabel(null)));

        var withProject = new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
            ProjectLabel = "  Project Alpha  ",
            VisaTypeLabel = "Work",
        };
        Assert.Equal("Project Alpha", ApplicationProfilePickerCaseSummaryRecentHelper.PrimaryLabel(withProject));

        var visaOnly = new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
            VisaTypeLabel = "  Invitation  ",
        };
        Assert.Equal("Invitation", ApplicationProfilePickerCaseSummaryRecentHelper.PrimaryLabel(visaOnly));

        var empty = new ApplicationProfilePickerCaseSummaryRecentEntry { Id = Guid.NewGuid() };
        Assert.False(string.IsNullOrWhiteSpace(
            ApplicationProfilePickerCaseSummaryRecentHelper.PrimaryLabel(empty)));
    }

    [Fact]
    public void MetaLine_joins_visa_type_and_period()
    {
        Assert.Equal(string.Empty, ApplicationProfilePickerCaseSummaryRecentHelper.MetaLine(null));

        var both = new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
            VisaTypeLabel = "Work",
            PeriodLabel = "30 days",
        };
        Assert.Equal("Work · 30 days", ApplicationProfilePickerCaseSummaryRecentHelper.MetaLine(both));

        var periodOnly = new ApplicationProfilePickerCaseSummaryRecentEntry
        {
            Id = Guid.NewGuid(),
            PeriodLabel = "90 days",
        };
        Assert.Equal("90 days", ApplicationProfilePickerCaseSummaryRecentHelper.MetaLine(periodOnly));
    }

    [Fact]
    public void Deserialize_returns_empty_for_blank_or_malformed_json()
    {
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(null));
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(" "));
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize("{not-json"));
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize("[]"));
    }

    [Fact]
    public void CreateFromFields_captures_labels_and_skips_em_dash_display()
    {
        var usedAt = new DateTime(2026, 11, 5, 10, 0, 0, DateTimeKind.Utc);
        var entry = ApplicationProfilePickerCaseSummaryRecentHelper.CreateFromFields(
            [
                Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, "Alpha"),
                Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaType, "—"),
                Field(ApplicationWorkspaceCaseHeaderFieldsHelper.VisaPeriod, "30 days"),
            ],
            companyLabel: "  Calik  ",
            usedAtUtc: usedAt);

        Assert.Equal(usedAt, entry.UsedAtUtc);
        Assert.Equal("Alpha", entry.ProjectLabel);
        Assert.Equal("Calik", entry.CompanyLabel);
        Assert.Equal(string.Empty, entry.VisaTypeLabel);
        Assert.Equal("30 days", entry.PeriodLabel);
        Assert.Contains(
            entry.Updates,
            u => u.Key == ApplicationWorkspaceCaseHeaderFieldsHelper.Project);
    }

    private static ApplicationWorkspaceCaseHeaderField Field(string key, string display) =>
        new()
        {
            Key = key,
            DisplayValue = display,
            Value = display,
            Kind = ApplicationWorkspaceCaseHeaderFieldKind.ShortText,
        };
}

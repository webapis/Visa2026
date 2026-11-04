using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssuedExtensionDefaultsHelperTests
{
    [Fact]
    public void TryAddVisaPeriod_AddsMonthsForAyUnit()
    {
        var period = new VisaPeriod { PdfForm__Code = "AY", PdfForm_Count = 6, LocalizationKey = "Month6" };
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(new DateTime(2024, 11, 4), period, out var end));
        Assert.Equal(new DateTime(2025, 5, 4), end);
    }

    [Fact]
    public void TryAddVisaPeriod_AddsDaysForGunUnit()
    {
        var period = new VisaPeriod { PdfForm__Code = "GUN", PdfForm_Count = 30, LocalizationKey = "Day30" };
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(new DateTime(2026, 1, 1), period, out var end));
        Assert.Equal(new DateTime(2026, 1, 31), end);
    }

    [Fact]
    public void TryAddVisaPeriod_AddsYearsForYylUnit()
    {
        var period = new VisaPeriod { PdfForm__Code = "YYL", PdfForm_Count = 1, LocalizationKey = "Year1" };
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(new DateTime(2026, 3, 15), period, out var end));
        Assert.Equal(new DateTime(2027, 3, 15), end);
    }

    [Fact]
    public void TryAddVisaPeriod_RejectsPerWorkPermitZeroCount()
    {
        var period = new VisaPeriod { PdfForm_Count = 0, LocalizationKey = "PerWorkPermit" };
        Assert.False(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(new DateTime(2026, 1, 1), period, out _));
    }

    [Fact]
    public void TryAddVisaPeriod_FallsBackToMonthsFromLocalizationKey()
    {
        var period = new VisaPeriod { PdfForm_Count = 3, LocalizationKey = "Month3" };
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(new DateTime(2026, 1, 10), period, out var end));
        Assert.Equal(new DateTime(2026, 4, 10), end);
    }

    [Fact]
    public void TryComputeExtensionDates_UsesLinkedEndPlusPeriod()
    {
        var period = new VisaPeriod { PdfForm__Code = "AY", PdfForm_Count = 6 };
        var linkedEnd = new DateTime(2024, 11, 4);
        var issue = new DateTime(2024, 11, 1);

        Assert.True(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            linkedEnd, issue, period, out var start, out var end, out var note));

        Assert.Equal(linkedEnd, start);
        Assert.Equal(new DateTime(2025, 5, 4), end);
        Assert.Contains("linked end", note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryComputeExtensionDates_ClampsStartToIssue_KeepsEndFromLinked()
    {
        var period = new VisaPeriod { PdfForm__Code = "AY", PdfForm_Count = 6 };
        var linkedEnd = new DateTime(2024, 10, 1);
        var issue = new DateTime(2024, 11, 1);

        Assert.True(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            linkedEnd, issue, period, out var start, out var end, out _));

        Assert.Equal(issue, start);
        Assert.Equal(new DateTime(2025, 4, 1), end);
    }

    [Fact]
    public void TryComputeExtensionDates_WhenNoLinkedEnd_FailsWithNote()
    {
        Assert.False(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            null, DateTime.Today, new VisaPeriod { PdfForm__Code = "AY", PdfForm_Count = 6 },
            out _, out _, out var note));
        Assert.Contains("No linked", note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveWorkPermitLocations_PrefersLinkedThenCase()
    {
        var linked = new WorkPermitItem { WorkPermittedLocations = "Mary etraby" };
        Assert.Equal("Mary etraby", IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(linked, "Ahal"));
        Assert.Equal("Ahal", IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(
            new WorkPermitItem { WorkPermittedLocations = "  " }, "Ahal"));
        Assert.Equal("Ahal", IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(null, "Ahal"));
    }
}
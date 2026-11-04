using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Edge cases for extension visa/WP date prefill: missing period, localization-key units,
/// legacy blank-code months, and linked-document resolve null gates.
/// </summary>
public class IssuedExtensionDefaultsEdgeTests
{
    [Fact]
    public void TryAddVisaPeriod_UsesDayAndYearLocalizationKeyPrefixes()
    {
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(
            new DateTime(2026, 1, 1),
            new VisaPeriod { PdfForm_Count = 10, LocalizationKey = "Day10" },
            out var dayEnd));
        Assert.Equal(new DateTime(2026, 1, 11), dayEnd);

        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(
            new DateTime(2026, 1, 1),
            new VisaPeriod { PdfForm_Count = 2, LocalizationKey = "Year2" },
            out var yearEnd));
        Assert.Equal(new DateTime(2028, 1, 1), yearEnd);
    }

    [Fact]
    public void TryAddVisaPeriod_BlankCodeWithCount_TreatsAsMonths()
    {
        Assert.True(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(
            new DateTime(2026, 2, 15),
            new VisaPeriod { PdfForm__Code = "  ", PdfForm_Count = 4 },
            out var end));
        Assert.Equal(new DateTime(2026, 6, 15), end);
    }

    [Fact]
    public void TryAddVisaPeriod_UnknownUnit_Fails()
    {
        Assert.False(IssuedExtensionDefaultsHelper.TryAddVisaPeriod(
            new DateTime(2026, 1, 1),
            new VisaPeriod { PdfForm__Code = "WEEK", PdfForm_Count = 2, LocalizationKey = "Other" },
            out _));
    }

    [Fact]
    public void TryComputeExtensionDates_MissingPeriod_ReturnsStartWithoutEnd()
    {
        var linkedEnd = new DateTime(2024, 11, 4);
        Assert.True(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            linkedEnd,
            issueOrToday: new DateTime(2024, 11, 1),
            period: null,
            out var start,
            out var end,
            out var note));

        Assert.Equal(linkedEnd, start);
        Assert.Null(end);
        Assert.Contains("enter End", note, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("period", note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryComputeExtensionDates_WhenLinkedPlusPeriodNotAfterClampedStart_UsesClampedPlusPeriod()
    {
        // linked end before issue → start clamps to issue; linked+period may land on/before start
        // so End must come from clamped start + period.
        var period = new VisaPeriod { PdfForm__Code = "GUN", PdfForm_Count = 1 };
        var linkedEnd = new DateTime(2024, 10, 31);
        var issue = new DateTime(2024, 11, 1);

        Assert.True(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            linkedEnd, issue, period, out var start, out var end, out _));

        Assert.Equal(issue, start);
        Assert.Equal(new DateTime(2024, 11, 2), end);
    }

    [Fact]
    public void TryResolveLinkedVisa_NullObjectSpaceOrPerson_ReturnsNull()
    {
        Assert.Null(IssuedExtensionDefaultsHelper.TryResolveLinkedVisa(null!, null, null));
        Assert.Null(IssuedExtensionDefaultsHelper.TryResolveLinkedVisa(
            null!, new ApplicationProfileInstance(), null));
        Assert.Null(IssuedExtensionDefaultsHelper.TryResolveLinkedVisa(
            null!, null, new Person { ID = Guid.NewGuid() }));
    }

    [Fact]
    public void TryResolveLinkedWorkPermitItem_NullPerson_ReturnsNull()
    {
        Assert.Null(IssuedExtensionDefaultsHelper.TryResolveLinkedWorkPermitItem(
            null!, new ApplicationProfileInstance(), null));
    }

    [Fact]
    public void ResolveWorkPermitLocations_TrimsAndFallsBackToEmpty()
    {
        Assert.Equal(string.Empty, IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(null, null));
        Assert.Equal(string.Empty, IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(
            new WorkPermitItem { WorkPermittedLocations = "\t" }, "  "));
        Assert.Equal("Ahal", IssuedExtensionDefaultsHelper.ResolveWorkPermitLocations(
            null, " Ahal "));
    }
}

using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.PreviewSlot;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class IssueIssuedHeaderWorkPermitComposeTests
{
    [Fact]
    public void TryCopyDatesFromLastWorkPermit_CopiesWhenStillValid()
    {
        var last = new WorkPermitItem
        {
            StartDate = new DateTime(2026, 1, 10),
            ExpirationDate = new DateTime(2027, 1, 9),
        };

        Assert.True(IssueIssuedHeaderComposeService.TryCopyDatesFromLastWorkPermit(
            last,
            new DateTime(2026, 8, 26),
            out var start,
            out var end));
        Assert.Equal(new DateTime(2026, 1, 10), start);
        Assert.Equal(new DateTime(2027, 1, 9), end);
    }

    [Fact]
    public void TryCopyDatesFromLastWorkPermit_RejectsExpired()
    {
        var last = new WorkPermitItem
        {
            StartDate = new DateTime(2024, 1, 10),
            ExpirationDate = new DateTime(2026, 1, 9),
        };

        Assert.False(IssueIssuedHeaderComposeService.TryCopyDatesFromLastWorkPermit(
            last,
            new DateTime(2026, 8, 26),
            out _,
            out _));
    }

    [Fact]
    public void IsWorkPermitCardComplete_RequiresItemFields()
    {
        var row = new IssueIssuedHeaderPersonLineDraft { PersonName = "Serdar" };
        Assert.False(IssueIssuedHeaderComposeService.IsWorkPermitCardComplete(row));

        row.ItemNumber = "WP-1";
        row.ASNumber = "AS-1";
        row.PositionId = Guid.NewGuid();
        row.PassportId = Guid.NewGuid();
        row.ItemStartDate = new DateTime(2026, 1, 10);
        row.ItemExpirationDate = new DateTime(2027, 1, 9);
        row.WorkPermittedLocations = "Ahal";
        Assert.True(IssueIssuedHeaderComposeService.IsWorkPermitCardComplete(row));
    }

    [Fact]
    public void TryComputeExtensionDates_FromLinkedWorkPermitEnd()
    {
        var period = new VisaPeriod { PdfForm__Code = "AY", PdfForm_Count = 6 };
        var linkedEnd = new DateTime(2025, 1, 21);

        Assert.True(IssuedExtensionDefaultsHelper.TryComputeExtensionDates(
            linkedEnd,
            new DateTime(2024, 10, 20),
            period,
            out var start,
            out var end,
            out _));

        Assert.Equal(linkedEnd, start);
        Assert.Equal(new DateTime(2025, 7, 21), end);
    }
}
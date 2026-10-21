using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Active/Expired Invitation NOT text and CSS buckets drive Excel day coloring.
/// </summary>
public class InvitationRemainingDaysNoteCssTests
{
    [Fact]
    public void FormatRemainingDaysNote_Blank_WhenExpirationMissingOrLegacy()
    {
        Assert.Equal(string.Empty, VwRdInvitationReady.FormatRemainingDaysNote(null));
        Assert.Equal(string.Empty, VwRdInvitationReady.FormatRemainingDaysNote(new DateTime(1900, 1, 1)));
        Assert.Equal(string.Empty, VwRdInvitationReady.RemainingDaysCssClass(null));
    }

    [Fact]
    public void FormatRemainingDaysNote_UsesTodayRelativeDayCount()
    {
        var expiration = DateTime.Today.AddDays(12);
        Assert.Equal(
            "geçerli gün sayı : 12",
            VwRdInvitationReady.FormatRemainingDaysNote(expiration));
        Assert.Equal(
            "geçerli gün sayı : -3",
            VwRdInvitationReady.FormatRemainingDaysNote(DateTime.Today.AddDays(-3)));
    }

    [Theory]
    [InlineData(-1, "rd-note--expired")]
    [InlineData(0, "rd-note--low")]
    [InlineData(13, "rd-note--low")]
    [InlineData(14, "rd-note--soon")]
    [InlineData(29, "rd-note--soon")]
    [InlineData(30, "rd-note--mid")]
    [InlineData(89, "rd-note--mid")]
    [InlineData(90, "rd-note--far")]
    public void RemainingDaysCssClass_BucketsMatchExcelColorThresholds(int daysFromToday, string expected)
    {
        Assert.Equal(
            expected,
            VwRdInvitationReady.RemainingDaysCssClass(DateTime.Today.AddDays(daysFromToday)));
    }

    [Fact]
    public void ReadyAndExpiredViews_ExposeSameNoteHelper()
    {
        var ready = new VwRdInvitationReady { ExpirationDate = DateTime.Today.AddDays(5) };
        var expired = new VwRdInvitationExpired { ExpirationDate = DateTime.Today.AddDays(5) };

        Assert.Equal(ready.RemainingDaysNote, expired.RemainingDaysNote);
        Assert.Equal("geçerli gün sayı : 5", ready.RemainingDaysNote);
    }
}

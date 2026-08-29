using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class PdfPackagingNotesLocalizationTests
{
    [Fact]
    public void SlotLabel_Current_DiffersFromPrevious()
    {
        var current = PdfPackagingNotesLocalization.SlotLabel("en-US", "Current");
        var previous = PdfPackagingNotesLocalization.SlotLabel("en-US", "Previous");
        var otherCasing = PdfPackagingNotesLocalization.SlotLabel("en-US", "current");

        Assert.False(string.IsNullOrWhiteSpace(current));
        Assert.False(string.IsNullOrWhiteSpace(previous));
        Assert.Equal(current, otherCasing);
        Assert.NotEqual(current, previous);
    }

    [Fact]
    public void FormatGap_SubstitutesArgsForCulture()
    {
        var text = PdfPackagingNotesLocalization.FormatGap(
            "en-US",
            "Pdf.Packaging.BatchId",
            "batch-42");

        Assert.Contains("batch-42", text, StringComparison.Ordinal);
    }
}

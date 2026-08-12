using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class PersonDocumentCopiesLocalizationTests
{
    [Theory]
    [InlineData(null, "Passport —")]
    [InlineData("", "Passport —")]
    [InlineData("   ", "Passport —")]
    [InlineData("AB123456", "Passport AB123456")]
    [InlineData("  AB123456  ", "Passport AB123456")]
    public void FormatPassportRecord_UsesEmDashForBlank(string number, string expected)
    {
        Assert.Equal(expected, PersonDocumentCopiesLocalization.FormatPassportRecord(number));
    }

    [Theory]
    [InlineData(null, "Visa —")]
    [InlineData("V-9", "Visa V-9")]
    public void FormatVisaRecord_BlankAndValue(string number, string expected)
    {
        Assert.Equal(expected, PersonDocumentCopiesLocalization.FormatVisaRecord(number));
    }

    [Fact]
    public void FormatEducationAndMedical_TrimCaption()
    {
        Assert.Equal("Education Bachelor", PersonDocumentCopiesLocalization.FormatEducationRecord("  Bachelor  "));
        Assert.Equal("Medical record —", PersonDocumentCopiesLocalization.FormatMedicalRecord(null));
    }

    [Fact]
    public void CurrentBadge_DefaultCulture()
    {
        Assert.Equal("Current", PersonDocumentCopiesLocalization.CurrentBadge("en-US"));
        Assert.Equal("Güncel", PersonDocumentCopiesLocalization.CurrentBadge("tr-TR"));
    }
}

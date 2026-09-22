using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class VisaUiMessagesNormalizeCultureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("zz-ZZ")]
    [InlineData("not-a-culture")]
    public void NormalizeCultureName_BlankOrUnsupported_ReturnsDefault(string cultureName)
    {
        Assert.Equal(VisaUiMessages.DefaultCultureName, VisaUiMessages.NormalizeCultureName(cultureName));
    }

    [Theory]
    [InlineData("en-US", "en-US")]
    [InlineData("en-us", "en-US")]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("tr-tr", "tr-TR")]
    [InlineData("tk-TM", "tk-TM")]
    [InlineData("ru-RU", "ru-RU")]
    public void NormalizeCultureName_ExactSupported_CanonicalizesCasing(string input, string expected)
    {
        Assert.Equal(expected, VisaUiMessages.NormalizeCultureName(input));
    }

    [Theory]
    [InlineData("en", "en-US")]
    [InlineData("tr", "tr-TR")]
    [InlineData("tk", "tk-TM")]
    [InlineData("ru", "ru-RU")]
    [InlineData("en-GB", "en-US")]
    [InlineData("tr-CY", "tr-TR")]
    public void NormalizeCultureName_LanguageFallback_MapsToSupportedCulture(string input, string expected)
    {
        Assert.Equal(expected, VisaUiMessages.NormalizeCultureName(input));
    }

    [Fact]
    public void PdfPackagingNotesCultureResolver_UsesRequestedCultureWhenPresent()
    {
        Assert.Equal(
            "tr-TR",
            PdfPackagingNotesCultureResolver.Resolve(objectSpace: null!, requestedByUserName: "anyone", requestedCulture: "tr"));
    }

    [Fact]
    public void PdfPackagingNotesCultureResolver_BlankUserWithoutCulture_ReturnsDefault()
    {
        Assert.Equal(
            VisaUiMessages.DefaultCultureName,
            PdfPackagingNotesCultureResolver.Resolve(objectSpace: null!, requestedByUserName: null));
        Assert.Equal(
            VisaUiMessages.DefaultCultureName,
            PdfPackagingNotesCultureResolver.Resolve(objectSpace: null!, requestedByUserName: "  "));
    }
}

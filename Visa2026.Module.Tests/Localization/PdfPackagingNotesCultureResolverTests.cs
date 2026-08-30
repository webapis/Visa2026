using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public sealed class PdfPackagingNotesCultureResolverTests
{
    // Short-circuit paths never touch objectSpace — null is intentional for isolation.

    [Theory]
    [InlineData("tr-TR", "tr-TR")]
    [InlineData("TR-tr", "tr-TR")]
    [InlineData("tk", "tk-TM")]
    [InlineData("ru-RU", "ru-RU")]
    [InlineData("en-US", "en-US")]
    [InlineData("xx-YY", "en-US")]
    public void Resolve_requestedCulture_shortCircuits(string requested, string expected)
    {
        var actual = PdfPackagingNotesCultureResolver.Resolve(
            objectSpace: null!,
            requestedByUserName: "ignored",
            requestedCulture: requested);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Resolve_whitespace_requestedCulture_falls_through_to_blank_user_default()
    {
        // Whitespace requestedCulture is treated as unset (IsNullOrWhiteSpace).
        var actual = PdfPackagingNotesCultureResolver.Resolve(
            objectSpace: null!,
            requestedByUserName: null,
            requestedCulture: "   ");

        Assert.Equal(VisaUiMessages.DefaultCultureName, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_blank_user_returns_default_without_lookup(string? userName)
    {
        var actual = PdfPackagingNotesCultureResolver.Resolve(
            objectSpace: null!,
            requestedByUserName: userName,
            requestedCulture: null);

        Assert.Equal(VisaUiMessages.DefaultCultureName, actual);
    }
}

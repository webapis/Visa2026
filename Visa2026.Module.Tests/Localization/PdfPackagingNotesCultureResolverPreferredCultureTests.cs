using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

/// <summary>
/// PACKAGING_NOTES culture from the queuing user's PreferredCulture (OS path).
/// Filename distinct from open PR #44 short-circuit tests.
/// </summary>
public class PdfPackagingNotesCultureResolverPreferredCultureTests
{
    [Fact]
    public void Resolve_RequestedCultureWinsOverUserPreference()
    {
        var space = CultureQueryObjectSpaceStub.Create(s =>
            s.Seed(new ApplicationUser { UserName = "officer", PreferredCulture = "ru-RU" }));

        var culture = PdfPackagingNotesCultureResolver.Resolve(space, "officer", "en-US");

        Assert.Equal("en-US", culture);
    }

    [Fact]
    public void Resolve_BlankUser_ReturnsDefaultCulture()
    {
        var space = CultureQueryObjectSpaceStub.Create();

        Assert.Equal(
            VisaUiMessages.DefaultCultureName,
            PdfPackagingNotesCultureResolver.Resolve(space, "  "));
    }

    [Fact]
    public void Resolve_UserPreferredCulture_IsNormalized()
    {
        var space = CultureQueryObjectSpaceStub.Create(s =>
            s.Seed(new ApplicationUser { UserName = "Serdar", PreferredCulture = "tr" }));

        var culture = PdfPackagingNotesCultureResolver.Resolve(space, "serdar");

        Assert.Equal("tr-TR", culture);
    }

    [Fact]
    public void Resolve_UnknownUser_ReturnsDefaultCulture()
    {
        var space = CultureQueryObjectSpaceStub.Create(s =>
            s.Seed(new ApplicationUser { UserName = "other", PreferredCulture = "en-US" }));

        Assert.Equal(
            VisaUiMessages.DefaultCultureName,
            PdfPackagingNotesCultureResolver.Resolve(space, "missing"));
    }

    [Fact]
    public void Resolve_UserWithBlankPreferredCulture_ReturnsDefault()
    {
        var space = CultureQueryObjectSpaceStub.Create(s =>
            s.Seed(new ApplicationUser { UserName = "officer", PreferredCulture = "   " }));

        Assert.Equal(
            VisaUiMessages.DefaultCultureName,
            PdfPackagingNotesCultureResolver.Resolve(space, "officer"));
    }
}

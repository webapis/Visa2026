#nullable enable

using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Isolated place-word yellows are not placeholders; Continue must not stay blocked when
/// the director title (ACPOS) is already mapped.
/// </summary>
public class ScanOfficialLetterSignatoryLocationPrefixTests
{
    [Theory]
    [InlineData("Türkmenistandaky", true)]
    [InlineData("turkmenistandaky", true)]
    [InlineData("Türkmenstandaky", true)] // common fold typo accepted by matcher
    [InlineData("Türkmenistandaky şahamçasynyň müdiri", false)]
    [InlineData("şahamçasynyň müdiri", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void LooksLikeSignatoryLocationPrefix_only_exact_place_word(string? text, bool expected) =>
        Assert.Equal(expected, ScanOfficialLetterHints.LooksLikeSignatoryLocationPrefix(text));
}

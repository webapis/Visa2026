using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateTextNormalizerTests
{
    [Theory]
    [InlineData("  Aýnabat   Meredowa ", "aýnabat meredowa")]
    [InlineData("Iş\tRugsatnamasy", "iş rugsatnamasy")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_trims_collapses_and_lowercases(string? input, string expected) =>
        Assert.Equal(expected, TemplateTextNormalizer.Normalize(input));

    [Theory]
    [InlineData("Aýnabat", "aynabat")]
    [InlineData("Işçi", "isci")]
    [InlineData("Aşgabat", "asgabat")]
    [InlineData("Türkmenistan", "turkmenistan")]
    [InlineData("Ýaşaýyş", "yasayys")]
    public void NormalizeFolded_folds_turkmen_diacritics(string input, string expected) =>
        Assert.Equal(expected, TemplateTextNormalizer.NormalizeFolded(input));

    [Fact]
    public void Casing_folds_with_invariant_rules_so_dotted_i_stays_stable() =>
        Assert.Equal(TemplateTextNormalizer.NormalizeFolded("ISLEG"), TemplateTextNormalizer.NormalizeFolded("isleg"));

    [Theory]
    [InlineData("T-1234567", "t1234567")]
    [InlineData("T 1234 567", "t1234567")]
    [InlineData("AB/12.34", "ab1234")]
    public void NormalizeIdentifier_drops_separators(string input, string expected) =>
        Assert.Equal(expected, TemplateTextNormalizer.NormalizeIdentifier(input));

    [Theory]
    [InlineData("12", false)]
    [InlineData("abc", true)]
    [InlineData("", false)]
    public void IsMatchable_requires_the_minimum_length(string input, bool expected) =>
        Assert.Equal(expected, TemplateTextNormalizer.IsMatchable(input));
}

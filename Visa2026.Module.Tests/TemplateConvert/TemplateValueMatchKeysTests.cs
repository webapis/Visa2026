using Visa2026.Module.Services.TemplateConvert;
using Xunit;

namespace Visa2026.Module.Tests.TemplateConvert;

public class TemplateValueMatchKeysTests
{
    [Theory]
    [InlineData("PFN", "Aýnabat Meredowa", ValueKind.PersonName)]
    [InlineData("PMNM", "Mine", ValueKind.PersonName)]
    [InlineData("PSEF", "Ali Enes Yetkin", ValueKind.PersonName)]
    [InlineData("PPN", "T 12345678", ValueKind.Identifier)]
    [InlineData("PDBT", "18.01.1977", ValueKind.Date)]
    [InlineData("CSAL", "5000", ValueKind.Number)]
    [InlineData("POSN", "Taslamanyň dolandyryş müdiri", ValueKind.Text)]
    public void Classify_uses_short_code_sets_then_value_shape(string shortCode, string value, ValueKind expected) =>
        Assert.Equal(expected, TemplateValueMatchKeys.Classify(shortCode, value));

    /// <summary>A date-looking identifier stays an identifier: the short-code set wins.</summary>
    [Fact]
    public void Identifier_codes_are_not_reclassified_as_dates() =>
        Assert.Equal(ValueKind.Identifier, TemplateValueMatchKeys.Classify("VNUM", "20.01.2026"));

    [Fact]
    public void Dates_match_every_rendering()
    {
        var keys = TemplateValueMatchKeys.Build("18.01.1977", ValueKind.Date);

        Assert.Contains("18.01.1977", keys);
        Assert.Contains("18/01/1977", keys);
        Assert.Contains("1977-01-18", keys);
        Assert.Contains("18.1.1977", keys);
    }

    [Fact]
    public void Dates_match_a_turkmen_long_form()
    {
        var keys = TemplateValueMatchKeys.Build("20.08.2026", ValueKind.Date);

        Assert.Contains("20 awgust 2026", keys);
    }

    [Fact]
    public void Identifiers_ignore_spaces_and_hyphens()
    {
        var keys = TemplateValueMatchKeys.Build("T 12345-678", ValueKind.Identifier);

        Assert.Contains("t12345678", keys);
    }

    [Fact]
    public void Person_names_match_both_word_orders()
    {
        var keys = TemplateValueMatchKeys.Build("Amanov Dowletmyrat", ValueKind.PersonName);

        Assert.Contains("amanov dowletmyrat", keys);
        Assert.Contains("dowletmyrat amanov", keys);
    }

    /// <summary>Turkmen diacritics fold so a document spelling without them still matches.</summary>
    [Fact]
    public void Person_names_fold_diacritics()
    {
        var keys = TemplateValueMatchKeys.Build("Aýnabat Şirmedowa", ValueKind.PersonName);

        Assert.Contains("aynabat sirmedowa", keys);
    }

    [Theory]
    [InlineData("1 500,50", 1500.50)]
    [InlineData("1,500.50", 1500.50)]
    [InlineData("1500", 1500)]
    [InlineData("1.500,00", 1500)]
    public void Numbers_accept_either_separator_convention(string value, double expected)
    {
        Assert.True(TemplateValueMatchKeys.TryParseNumber(value, out var parsed));
        Assert.Equal((decimal)expected, parsed);
    }

    [Fact]
    public void Numbers_share_a_canonical_key_across_formats()
    {
        var withSpace = TemplateValueMatchKeys.Build("1 500", ValueKind.Number);
        var withComma = TemplateValueMatchKeys.Build("1,500", ValueKind.Number);

        Assert.Contains("1500", withSpace);
        Assert.Contains("1500", withComma);
    }

    [Theory]
    [InlineData("not a date")]
    [InlineData("18.01")]
    [InlineData("")]
    public void TryParseDate_rejects_non_dates(string value) =>
        Assert.False(TemplateValueMatchKeys.TryParseDate(value, out _));

    [Fact]
    public void Keys_below_the_minimum_length_are_dropped() =>
        Assert.Empty(TemplateValueMatchKeys.Build("ab", ValueKind.Text));
}

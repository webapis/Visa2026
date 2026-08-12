using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014SalaryAmountNormalizerTests
{
    [Fact]
    public void TryNormalize_Empty_ReturnsFalse()
    {
        Assert.False(Visa2014SalaryAmountNormalizer.TryNormalize(null, out var amount, out var note));
        Assert.Equal(string.Empty, amount);
        Assert.Equal("empty", note);

        Assert.False(Visa2014SalaryAmountNormalizer.TryNormalize("   ", out amount, out note));
        Assert.Equal("empty", note);
    }

    [Theory]
    [InlineData("1500", "1500", "plain")]
    [InlineData("1.667,00", "1.667.00", "normalized_separators")]
    [InlineData("2,500.00", "2,500.00", "plain")]
    public void TryNormalize_PlainAmounts(string raw, string expectedAmount, string expectedNote)
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(raw, out var amount, out var note));
        Assert.Equal(expectedAmount, amount);
        Assert.Equal(expectedNote, note);
    }

    [Fact]
    public void TryNormalize_ExtractsLargestTokenFromSentence()
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(
            "Salary approx 1.200,00 dtm monthly (was 800)",
            out var amount,
            out var note));

        Assert.Equal("1.200.00", amount);
        Assert.Equal("extracted_from_sentence", note);
    }

    [Fact]
    public void TryNormalize_NoToken_ReturnsFalse()
    {
        Assert.False(Visa2014SalaryAmountNormalizer.TryNormalize("no digits here", out var amount, out var note));
        Assert.Equal(string.Empty, amount);
        Assert.Equal("no_amount_token", note);
    }

    [Fact]
    public void TryNormalize_TruncatesLongExtractedAmount()
    {
        // Letters force extract path; European grouping yields a token longer than 32 after normalize.
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(
            "pay 9.999.999.999.999.999.999.999,99 monthly",
            out var amount,
            out var note));

        Assert.Equal(32, amount.Length);
        Assert.Equal("extracted_truncated", note);
        Assert.StartsWith("9.999.999.999.999.999.999.999", amount);
    }

    [Fact]
    public void ResolveCurrency_AlwaysUsd()
    {
        Assert.Equal("USD", Visa2014SalaryAmountNormalizer.ResolveCurrency(null));
        Assert.Equal("USD", Visa2014SalaryAmountNormalizer.ResolveCurrency("1500 dtm"));
        Assert.Equal("USD", Visa2014SalaryAmountNormalizer.ResolveCurrency("EUR 2000"));
    }
}

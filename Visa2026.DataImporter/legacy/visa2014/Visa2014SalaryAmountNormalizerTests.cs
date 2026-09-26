using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Regression coverage for legacy Salary.Detail → EmployeeSalary.Amount after currency was removed.
/// Wrong parse notes or separators break Calik salary import rows.
/// </summary>
public sealed class Visa2014SalaryAmountNormalizerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryNormalize_empty_returns_false_with_empty_note(string? raw)
    {
        Assert.False(Visa2014SalaryAmountNormalizer.TryNormalize(raw, out var amount, out var note));
        Assert.Equal(string.Empty, amount);
        Assert.Equal("empty", note);
    }

    [Theory]
    [InlineData("1500", "1500", "plain")]
    [InlineData("  2 500  ", "2500", "normalized_separators")]
    [InlineData("1.667,00", "1.667.00", "normalized_separators")]
    public void TryNormalize_plain_amount_keeps_or_normalizes_separators(
        string raw,
        string expectedAmount,
        string expectedNote)
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(raw, out var amount, out var note));
        Assert.Equal(expectedAmount, amount);
        Assert.Equal(expectedNote, note);
        Assert.True(amount.Length <= 32);
    }

    [Fact]
    public void TryNormalize_extracts_best_token_from_sentence()
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(
            "Aylik hak: 12.500,50 manat",
            out var amount,
            out var note));

        Assert.Equal("12.500.50", amount);
        Assert.Equal("extracted_from_sentence", note);
    }

    [Fact]
    public void TryNormalize_prefers_larger_numeric_token_when_several_match()
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(
            "bonus 1.000,00 and salary 3.250,00",
            out var amount,
            out _));

        Assert.Equal("3.250.00", amount);
    }

    [Theory]
    [InlineData("no numbers here")]
    [InlineData("OID-ABC-XYZ")]
    public void TryNormalize_without_amount_token_returns_false(string raw)
    {
        Assert.False(Visa2014SalaryAmountNormalizer.TryNormalize(raw, out var amount, out var note));
        Assert.Equal(string.Empty, amount);
        Assert.Equal("no_amount_token", note);
    }

    [Fact]
    public void TryNormalize_truncates_extracted_amount_over_32_chars()
    {
        // Dotted thousand groups kept by NormalizeSeparators can exceed Amount max (32).
        const string raw = "amount 1.234.567.890.123.456.789.012.345.67";

        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(raw, out var amount, out var note));
        Assert.Equal(32, amount.Length);
        Assert.Equal("extracted_truncated", note);
    }

    [Fact]
    public void TryNormalize_letter_context_does_not_treat_whole_string_as_plain()
    {
        Assert.True(Visa2014SalaryAmountNormalizer.TryNormalize(
            "USD 1.200,00",
            out var amount,
            out var note));

        Assert.Equal("1.200.00", amount);
        Assert.Equal("extracted_from_sentence", note);
    }
}

using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacyTableColumnResolverTests
{
    [Theory]
    [InlineData("Content", "[Content]")]
    [InlineData("Göçürme", "[Göçürme]")]
    [InlineData("Odd]Name", "[Odd]]Name]")]
    [InlineData("a]]b", "[a]]]]b]")]
    public void Bracket_EscapesClosingBrackets(string columnName, string expected)
    {
        Assert.Equal(expected, Visa2014LegacyTableColumnResolver.Bracket(columnName));
    }
}

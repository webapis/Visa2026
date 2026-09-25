using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Catalog JSON bool parsing must stay aligned with DataImporter seed rows.
/// </summary>
public class DataImporterBoolParserTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("yes", true)]
    [InlineData("Yes", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    [InlineData("no", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("YES", false)]
    [InlineData("on", false)]
    public void IsTrue_accepts_only_documented_literals(string? raw, bool expected) =>
        Assert.Equal(expected, DataImporterBoolParser.IsTrue(raw));
}

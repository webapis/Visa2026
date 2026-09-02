using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class CommaSeparatedCatalogHelperMergeTests
{
    [Fact]
    public void MergeCatalogWithSelected_AddsMissingSelectedAndSorts()
    {
        var merged = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
            ["Beta", "Alpha"],
            ["gamma", " Alpha ", " ", "Beta"]);

        Assert.Equal(new[] { "Alpha", "Beta", "gamma" }, merged);
    }

    [Fact]
    public void MergeCatalogWithSelected_EmptySelected_ReturnsCatalogSorted()
    {
        var merged = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(
            ["z", "a"],
            Array.Empty<string>());

        Assert.Equal(new[] { "a", "z" }, merged);
    }
}

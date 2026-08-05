using System;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class CommaSeparatedCatalogHelperTests
{
    [Fact]
    public void MergeCatalogWithSelected_AddsMissingSelectedAndDedupesCaseInsensitively()
    {
        var catalog = new[] { "Ashgabat", "Mary" };
        var selected = new[] { "mary", " Balkan ", "", null!, "Dashoguz" };

        var merged = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(catalog, selected);

        Assert.Equal(new[] { "Ashgabat", "Balkan", "Dashoguz", "Mary" }, merged);
    }

    [Fact]
    public void MergeCatalogWithSelected_EmptySelected_ReturnsOrderedCatalog()
    {
        var catalog = new[] { "Zebra", "Apple" };
        var merged = CommaSeparatedCatalogHelper.MergeCatalogWithSelected(catalog, Array.Empty<string>());

        Assert.Equal(new[] { "Apple", "Zebra" }, merged);
    }

    [Fact]
    public void CatalogOperationResult_OkAndFail_CarryMessageAndUsage()
    {
        var ok = CatalogOperationResult.Ok("done", usageCount: 3);
        Assert.True(ok.Success);
        Assert.Equal("done", ok.Message);
        Assert.Equal(3, ok.UsageCount);

        var fail = CatalogOperationResult.Fail("CommaMultiSelect.Error.DuplicateName", usageCount: 1);
        Assert.False(fail.Success);
        Assert.Equal("CommaMultiSelect.Error.DuplicateName", fail.Message);
        Assert.Equal(1, fail.UsageCount);
    }
}

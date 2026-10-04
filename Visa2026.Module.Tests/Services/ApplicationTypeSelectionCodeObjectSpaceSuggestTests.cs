using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Tests.TestSupport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// ObjectSpace path for ministry SelectionCode clone suggestions.
/// Wrong group filtering reuses codes or returns null mid-group when cloning ApplicationType.
/// (Pure-string overload lives on an open coverage PR; this file covers the live OS API on master.)
/// </summary>
public sealed class ApplicationTypeSelectionCodeObjectSpaceSuggestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("70")]
    [InlineData("7001")]
    [InlineData("abc")]
    [InlineData("000")]
    [InlineData("900")]
    public void SuggestNext_InvalidSource_ReturnsNull(string? source)
    {
        var space = MultiTypeSeedObjectSpaceStub.Create();
        Assert.Null(ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(space, source));
    }

    [Fact]
    public void SuggestNext_EmptyGroup_ReturnsHighestCodeInHundreds()
    {
        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(new ApplicationType { SelectionCode = "650" });
        });

        Assert.Equal("799", ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(space, "701"));
    }

    [Fact]
    public void SuggestNext_SkipsUsedCodesDescending_WithinSameGroupOnly()
    {
        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(
                new ApplicationType { SelectionCode = "799" },
                new ApplicationType { SelectionCode = "798" },
                new ApplicationType { SelectionCode = "701" },
                new ApplicationType { SelectionCode = "650" });
        });

        Assert.Equal("797", ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(space, "701"));
    }

    [Fact]
    public void SuggestNext_IgnoresBlankAndShortCodesInQuery()
    {
        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            stub.Seed(
                new ApplicationType { SelectionCode = null },
                new ApplicationType { SelectionCode = "" },
                new ApplicationType { SelectionCode = "79" },
                new ApplicationType { SelectionCode = "799" });
        });

        Assert.Equal("798", ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(space, "701"));
    }

    [Fact]
    public void SuggestNext_FullGroup_ReturnsNull()
    {
        var space = MultiTypeSeedObjectSpaceStub.Create(stub =>
        {
            var types = Enumerable.Range(700, 100)
                .Select(n => new ApplicationType { SelectionCode = n.ToString("D3") })
                .ToArray();
            stub.Seed(types);
        });

        Assert.Null(ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(space, "701"));
    }
}

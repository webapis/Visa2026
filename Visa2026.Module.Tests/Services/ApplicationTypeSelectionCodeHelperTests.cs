using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Ministry SelectionCode clone suggestions — wrong group math reuses codes or returns null mid-group.
/// </summary>
public sealed class ApplicationTypeSelectionCodeHelperTests
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
        Assert.Null(ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(source, []));
    }

    [Fact]
    public void SuggestNext_EmptyGroup_ReturnsHighestCode()
    {
        var next = ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode("701", []);
        Assert.Equal("799", next);
    }

    [Fact]
    public void SuggestNext_SkipsUsedCodesDescending()
    {
        var next = ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(
            "701",
            ["799", "798", "701", "650"]);
        Assert.Equal("797", next);
    }

    [Fact]
    public void SuggestNext_IgnoresOtherHundredsGroups()
    {
        var next = ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode(
            "201",
            ["299", "201", "799"]);
        Assert.Equal("298", next);
    }

    [Fact]
    public void SuggestNext_FullGroup_ReturnsNull()
    {
        var used = Enumerable.Range(700, 100).Select(n => n.ToString("D3"));
        Assert.Null(ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode("701", used));
    }

    [Fact]
    public void SuggestNext_CaseInsensitiveUsedSet()
    {
        // Codes are digits; still ensure comparer does not double-count.
        var next = ApplicationTypeSelectionCodeHelper.SuggestNextSelectionCode("101", ["199"]);
        Assert.Equal("198", next);
    }
}

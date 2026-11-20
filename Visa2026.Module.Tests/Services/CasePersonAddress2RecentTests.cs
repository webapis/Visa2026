using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CasePersonAddress2RecentTests
{
    [Fact]
    public void NormalizeDisplay_RejectsBlankAndDash()
    {
        Assert.Null(CasePersonAddress2Recent.NormalizeDisplay(null));
        Assert.Null(CasePersonAddress2Recent.NormalizeDisplay("  "));
        Assert.Null(CasePersonAddress2Recent.NormalizeDisplay("—"));
        Assert.Equal("Line A", CasePersonAddress2Recent.NormalizeDisplay("  Line A  "));
    }

    [Fact]
    public void TextKey_IsStableFolded()
    {
        var a = CasePersonAddress2Recent.TextKey("Demo Address");
        var b = CasePersonAddress2Recent.TextKey("  demo address  ");
        Assert.False(string.IsNullOrEmpty(a));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Cap_IsSix() => Assert.Equal(6, CasePersonAddress2Recent.Cap);
}
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseLocationWpFormatTests
{
    [Fact]
    public void FormatChips_EmptyNone_KeepsMultiple()
    {
        var formatted = CaseLocationAutocomplete.FormatChips(
            new[] { "Farap etraby", "Garabogaz şäheri" },
            string.Empty);
        Assert.Equal("Farap etraby, Garabogaz şäheri", formatted);
        var parsed = CaseLocationAutocomplete.ParseChips(formatted, string.Empty);
        Assert.Equal(2, parsed.Count);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseLocationAutocompleteTests
{
    [Fact]
    public void NormalizeDisplay_CapsAt100()
    {
        var longText = new string('a', 150);
        Assert.Equal(CaseLocationAutocomplete.MaxLength, CaseLocationAutocomplete.NormalizeDisplay(longText).Length);
        Assert.Equal("foo bar", CaseLocationAutocomplete.NormalizeDisplay("  foo   bar  "));
    }

    [Fact]
    public void ParseAndFormat_RoundTrip_BorderZoneNone()
    {
        Assert.Empty(CaseLocationAutocomplete.ParseChips(CommaSeparatedSelectionHelper.NoneValue));
        Assert.Equal(
            CommaSeparatedSelectionHelper.NoneValue,
            CaseLocationAutocomplete.FormatChips(Array.Empty<string>()));

        var chips = CaseLocationAutocomplete.ParseChips("Mary, Aşgabat");
        Assert.Equal(2, chips.Count);
        Assert.Equal("Mary, Aşgabat", CaseLocationAutocomplete.FormatChips(chips));
    }

    [Fact]
    public void ParseAndFormat_WorkPermit_EmptyNone()
    {
        Assert.Empty(CaseLocationAutocomplete.ParseChips(null, string.Empty));
        Assert.Equal(string.Empty, CaseLocationAutocomplete.FormatChips(Array.Empty<string>(), string.Empty));
        var chips = CaseLocationAutocomplete.ParseChips("Lebap; Balkan", string.Empty);
        Assert.Equal(2, chips.Count);
        Assert.Equal("Lebap, Balkan", CaseLocationAutocomplete.FormatChips(chips, string.Empty));
    }

    [Fact]
    public void Search_ExcludesAlreadySelected()
    {
        var store = new List<CaseAddressAutocomplete.SavedEntry>
        {
            new("Aşgabat", 5, DateTime.UtcNow),
            new("Mary", 2, DateTime.UtcNow),
            new("Balkan", 1, DateTime.UtcNow),
        };
        var result = CaseLocationAutocomplete.Search("a", store, excludeLabels: new[] { "Aşgabat" });
        Assert.DoesNotContain(result.Items, i => CaseLocationAutocomplete.FoldKey(i.Text) == CaseLocationAutocomplete.FoldKey("Aşgabat"));
        Assert.Contains(result.Items, i => CaseLocationAutocomplete.FoldKey(i.Text) == CaseLocationAutocomplete.FoldKey("Balkan"));
    }
}

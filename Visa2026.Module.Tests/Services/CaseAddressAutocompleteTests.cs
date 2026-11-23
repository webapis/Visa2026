using System;
using System.Collections.Generic;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseAddressAutocompleteTests
{
    [Fact]
    public void FoldKey_MapsTurkmenDiacritics_LengthPreserving()
    {
        var folded = CaseAddressAutocomplete.FoldKey("Aşgabat ýoly");
        Assert.Equal("asgabat yoly", folded);
        Assert.Equal("Aşgabat ýoly".Length, folded.Length);
    }

    [Fact]
    public void Search_RequiresEveryToken_AndRanksStartsWithFirst()
    {
        var store = new List<CaseAddressAutocomplete.SavedEntry>
        {
            new("Garaşsyzlyk şaýoly 12, Aşgabat", 2, DateTime.UtcNow.AddDays(-2)),
            new("Aşgabat köçesi 5", 9, DateTime.UtcNow.AddDays(-1)),
            new("Mary welaýaty", 1, DateTime.UtcNow),
        };

        var result = CaseAddressAutocomplete.Search("asgabat", store);
        Assert.Equal(2, result.Total);
        Assert.Equal("Aşgabat köçesi 5", result.Items[0].Text);
    }

    [Fact]
    public void IsExactSaved_IgnoresDiacriticsAndWhitespace()
    {
        var store = new[]
        {
            new CaseAddressAutocomplete.SavedEntry("Aşgabat  ýoly", 1, DateTime.UtcNow),
        };
        Assert.True(CaseAddressAutocomplete.IsExactSaved("asgabat yoly", store));
        Assert.False(CaseAddressAutocomplete.IsExactSaved("mary", store));
    }

    [Fact]
    public void NormalizeDisplay_CollapsesWhitespace_AndCapsLength()
    {
        var longText = new string('a', 600);
        var normalized = CaseAddressAutocomplete.NormalizeDisplay("  foo   bar  \n baz  ");
        Assert.Equal("foo bar baz", normalized);
        Assert.Equal(CaseAddressAutocomplete.MaxLength, CaseAddressAutocomplete.NormalizeDisplay(longText).Length);
    }

    [Fact]
    public void ValidateDialogText_RejectsEmptyAndDuplicate()
    {
        var store = new[]
        {
            new CaseAddressAutocomplete.SavedEntry("Aşgabat ýoly", 1, DateTime.UtcNow),
        };
        Assert.Equal("Enter an address.", CaseAddressAutocomplete.ValidateDialogText("  ", store));
        Assert.Equal(
            "\u201casgabat yoly\u201d already exists in this list. Close this dialog and search for it.",
            CaseAddressAutocomplete.ValidateDialogText("asgabat yoly", store));
        Assert.Null(CaseAddressAutocomplete.ValidateDialogText("asgabat yoly", store, excludeText: "Aşgabat ýoly"));
        Assert.Null(CaseAddressAutocomplete.ValidateDialogText("Mary welaýaty", store));
    }

    [Fact]
    public void Search_ExcludesSelectedText()
    {
        var store = new List<CaseAddressAutocomplete.SavedEntry>
        {
            new("Aşgabat köçesi 5", 9, DateTime.UtcNow),
            new("Aşgabat ýoly 1", 2, DateTime.UtcNow),
        };
        var result = CaseAddressAutocomplete.Search("asgabat", store, excludeText: "Aşgabat köçesi 5");
        Assert.Equal(1, result.Total);
        Assert.Equal("Aşgabat ýoly 1", result.Items[0].Text);
    }
}
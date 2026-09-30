using System;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services.OfficerShell;

/// <summary>
/// Lightweight Seretmezlik template gates not covered by the existing merge/validate suite.
/// </summary>
public class ExclusionLetterBuilderTemplateGateTests
{
    [Fact]
    public void KindLabel_distinguishes_letter_and_roster()
    {
        Assert.Equal("Letter", ApplicationProfileInstanceExclusionLetterBuilder.KindLabel(
            ApplicationProfileInstanceExclusionTemplateKind.Letter));
        Assert.Equal("Roster (Sanaw)", ApplicationProfileInstanceExclusionLetterBuilder.KindLabel(
            ApplicationProfileInstanceExclusionTemplateKind.Roster));
    }

    [Theory]
    [InlineData("a.docx", true, false)]
    [InlineData("a.DOCX", true, false)]
    [InlineData("a.xlsx", false, true)]
    [InlineData("a.XLSX", false, true)]
    [InlineData("a.doc", false, false)]
    [InlineData(null, false, false)]
    public void IsWord_and_IsExcel_use_extension(string? name, bool word, bool excel)
    {
        Assert.Equal(word, ApplicationProfileInstanceExclusionLetterBuilder.IsWord(name));
        Assert.Equal(excel, ApplicationProfileInstanceExclusionLetterBuilder.IsExcel(name));
    }

    [Fact]
    public void ValidateTemplate_rejects_empty_content()
    {
        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(
            ApplicationProfileInstanceExclusionTemplateKind.Letter,
            "letter.docx",
            Array.Empty<byte>());

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, e => e.Contains("empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Placeholder_tokens_use_ds_or_row_prefix()
    {
        var header = ApplicationProfileInstanceExclusionLetterBuilder.Placeholders.First(p => !p.IsRow);
        var row = ApplicationProfileInstanceExclusionLetterBuilder.Placeholders.First(p => p.IsRow);

        Assert.Equal("{{ds." + header.Key + "}}", header.Token);
        Assert.Equal("{{." + row.Key + "}}", row.Token);
    }

    [Fact]
    public void BuildFileName_uses_draft_and_roster_prefix()
    {
        Assert.Equal(
            "Seretmezlik_draft.docx",
            ApplicationProfileInstanceExclusionLetterBuilder.BuildFileName(
                new ApplicationProfileInstanceExclusion { LetterNumber = "  " }));

        Assert.Equal(
            "Seretmezlik_sanaw_01.docx",
            ApplicationProfileInstanceExclusionLetterBuilder.BuildFileName(
                new ApplicationProfileInstanceExclusion { LetterNumber = "01" },
                ApplicationProfileInstanceExclusionTemplateKind.Roster));
    }

    [Fact]
    public void TurkmenDative_and_Genitive_return_empty_for_blank()
    {
        Assert.Equal(string.Empty, ApplicationProfileInstanceExclusionLetterBuilder.TurkmenDative("  "));
        Assert.Equal(string.Empty, ApplicationProfileInstanceExclusionLetterBuilder.TurkmenGenitive(null));
    }

    [Fact]
    public void BuildMergeData_empty_people_clears_excluded_count_text()
    {
        var exclusion = new ApplicationProfileInstanceExclusion
        {
            LetterNumber = "01/-01",
            LetterDate = new DateTime(2026, 9, 30),
            OriginalRosterCount = 5,
        };

        var data = ApplicationProfileInstanceExclusionLetterBuilder.BuildMergeData(exclusion, instance: null);

        Assert.Equal("0", data["ExcludedCount"]);
        Assert.Equal(string.Empty, data["ExcludedCountText"]);
        Assert.Equal("30.09.2026", data["LetterDate"]);
    }
}

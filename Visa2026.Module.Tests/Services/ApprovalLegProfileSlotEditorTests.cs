using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApprovalLegCatalog;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ApprovalLegProfileSlotEditorTests
{
    [Fact]
    public void AllocateUniqueCode_returns_preferred_when_free()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal("TE-EN", ApprovalLegProfileSlotEditor.AllocateUniqueCode("TE-EN", existing));
    }

    [Fact]
    public void AllocateUniqueCode_appends_suffix_when_taken()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TE-EN" };
        Assert.Equal("TE-EN-2", ApprovalLegProfileSlotEditor.AllocateUniqueCode("TE-EN", existing));
    }

    [Fact]
    public void AllocateUniqueCode_is_case_insensitive()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "te-en" };
        Assert.Equal("TE-EN-2", ApprovalLegProfileSlotEditor.AllocateUniqueCode("TE-EN", existing));
    }

    [Fact]
    public void AllocateUniqueCode_truncates_to_max_length()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var longCode = new string('A', 40);
        var allocated = ApprovalLegProfileSlotEditor.AllocateUniqueCode(longCode, existing);
        Assert.Equal(ApprovalLegProfileSlotEditor.CodeMaxLength, allocated.Length);
    }

    [Fact]
    public void MatchesSearch_filters_code_and_ministries()
    {
        var row = new ApprovalLegProfileSlotCatalogRow
        {
            Code = "TE-EN",
            MinistriesLabel = "Türkmenenergo-Energetika",
        };

        Assert.True(ApprovalLegProfileSlotEditor.MatchesSearch(row, null));
        Assert.True(ApprovalLegProfileSlotEditor.MatchesSearch(row, "te-en"));
        Assert.True(ApprovalLegProfileSlotEditor.MatchesSearch(row, "Energetika"));
        Assert.False(ApprovalLegProfileSlotEditor.MatchesSearch(row, "TNGZ"));
    }

    [Fact]
    public void TryNormalizeNewMinistry_trims_both_names()
    {
        Assert.True(ApprovalLegProfileSlotEditor.TryNormalizeNewMinistry(
            "  Türkmengaz  ",
            "  \"Türkmengaz\" döwlet konserni  ",
            out var shortName,
            out var official,
            out var error));
        Assert.Null(error);
        Assert.Equal("Türkmengaz", shortName);
        Assert.Equal("\"Türkmengaz\" döwlet konserni", official);
    }

    [Fact]
    public void TryNormalizeNewMinistry_requires_short_name()
    {
        Assert.False(ApprovalLegProfileSlotEditor.TryNormalizeNewMinistry(
            "  ",
            "Official",
            out _,
            out _,
            out var error));
        Assert.Equal(VisaUiMessages.Get("ApprovalLegProfile.Slot.ShortNameRequired"), error);
    }

    [Fact]
    public void TryNormalizeNewMinistry_requires_official_name()
    {
        Assert.False(ApprovalLegProfileSlotEditor.TryNormalizeNewMinistry(
            "Türkmengaz",
            " ",
            out _,
            out _,
            out var error));
        Assert.Equal(VisaUiMessages.Get("ApprovalLegProfile.Slot.OfficialNameRequired"), error);
    }

    [Fact]
    public void IsShortNameTaken_is_case_insensitive()
    {
        var existing = new[] { "Energetika", "TNGIZ" };
        Assert.True(ApprovalLegProfileSlotEditor.IsShortNameTaken(existing, "energetika"));
        Assert.False(ApprovalLegProfileSlotEditor.IsShortNameTaken(existing, "Türkmengaz"));
    }

    [Fact]
    public void ResolveCommitError_keeps_generic_for_unknown_failures()
    {
        var error = ApprovalLegProfileSlotEditor.ResolveCommitError(new InvalidOperationException("fk"));
        Assert.Equal(VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed"), error);
    }

    [Fact]
    public void ApplyScalars_writes_inactive()
    {
        var profile = new ApprovalLegProfile { IsActive = true, Code = "EN-AH-GU" };
        var draft = new ApprovalLegProfileSlotDraft
        {
            Code = "EN-AH-GU",
            IsActive = false,
            Legs =
            [
                new ApprovalLegProfileSlotLegDraft { Caption = "Energetika" },
                new ApprovalLegProfileSlotLegDraft { Caption = "Aşgabat häkimlik" },
                new ApprovalLegProfileSlotLegDraft { Caption = "Gurluşyk" },
            ],
        };

        ApprovalLegProfileSlotEditor.ApplyScalars(profile, draft);

        Assert.False(profile.IsActive);
        Assert.Equal("EN-AH-GU", profile.Code);
    }
}

#nullable enable

using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.TemplateScan;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Green highlighter people lines must become row PFN/RNUM tokens; yellow must stay untouched.
/// Wrong PreferRoster collapses header letter fields into the roster or skips numbered names.
/// </summary>
public class ScanRosterGreenMarkPreferRosterTests
{
    [Fact]
    public void PreferRoster_yellow_mark_returns_drafts_unchanged()
    {
        var drafts = new[] { Draft("{{ds.AFNUM}}", ScanFieldScope.Header) };
        var yellow = Green("1. Hayati Uyan", ScanOfficeMarkKind.Yellow);
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "AFNUM" };

        var next = ScanRosterGreenMark.PreferRoster(drafts, yellow, PlaceholderSet("PFN", "RNUM", "AFNUM"), used);

        Assert.Same(drafts[0], next[0]);
        Assert.Contains("AFNUM", used);
    }

    [Fact]
    public void PreferRoster_locked_drafts_are_left_alone()
    {
        var drafts = new[]
        {
            new ScanDetectedFieldDraft
            {
                FieldId = "locked",
                Box = ScanBoundingBox.FullPage,
                PageIndex = 0,
                LabelText = "1. Hayati Uyan",
                ProposedToken = "{{ds.CHFN}}",
                Scope = ScanFieldScope.Header,
                IsLocked = true,
            },
        };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CHFN" };

        var next = ScanRosterGreenMark.PreferRoster(
            drafts,
            Green("1. Hayati Uyan"),
            PlaceholderSet("PFN", "RNUM"),
            used);

        Assert.Same(drafts[0], next[0]);
        Assert.Contains("CHFN", used);
    }

    [Fact]
    public void PreferRoster_numbered_person_line_becomes_rnum_and_pfn_row_token()
    {
        var drafts = new[] { Draft("{{ds.CHFN}}", ScanFieldScope.Header) };
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CHFN" };

        var next = ScanRosterGreenMark.PreferRoster(
            drafts,
            Green("1. Hayati Uyan"),
            PlaceholderSet("PFN", "RNUM", "CHFN"),
            used);

        var person = Assert.Single(next);
        Assert.Equal(ScanFieldScope.Row, person.Scope);
        Assert.Equal(ScanFieldConfidence.High, person.Confidence);
        Assert.Equal("1. Hayati Uyan", person.LabelText);
        Assert.Equal("{{.RNUM}}. {{.PFN}}", person.ProposedToken);
        Assert.DoesNotContain("CHFN", used);
    }

    [Fact]
    public void PreferRoster_person_line_without_rnum_is_pfn_only()
    {
        var next = ScanRosterGreenMark.PreferRoster(
            [Draft("{{ds.CHFN}}", ScanFieldScope.Header)],
            Green("2) Aygul Meredova"),
            PlaceholderSet("PFN", "CHFN"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "CHFN" });

        Assert.Equal("{{.PFN}}", Assert.Single(next).ProposedToken);
    }

    [Fact]
    public void PreferRoster_column_header_blocks_person_line_and_rewrites_header_token_to_row()
    {
        var draft = Draft("{{ds.PFN}}", ScanFieldScope.Header, columnHeader: "Familiýasy");
        var next = ScanRosterGreenMark.PreferRoster(
            [draft],
            Green("Hayati Uyan"),
            PlaceholderSet("PFN"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var rewritten = Assert.Single(next);
        Assert.Equal("{{.PFN}}", rewritten.ProposedToken);
        Assert.Equal(ScanFieldScope.Row, rewritten.Scope);
        Assert.Equal(draft.FieldId, rewritten.FieldId);
    }

    [Fact]
    public void PreferRoster_non_person_green_rewrites_row_capable_header_codes()
    {
        var next = ScanRosterGreenMark.PreferRoster(
            [Draft("{{ds.PPN}}", ScanFieldScope.Header)],
            Green("S36133641"),
            PlaceholderSet("PPN"),
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var rewritten = Assert.Single(next);
        Assert.Equal("{{.PPN}}", rewritten.ProposedToken);
        Assert.Equal(ScanFieldScope.Row, rewritten.Scope);
    }

    [Fact]
    public void PreferRoster_keeps_true_header_only_codes()
    {
        var set = new ApplicationProfilePlaceholderSet
        {
            ApplicationProfileId = Guid.NewGuid(),
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = ApplicationProfileTemplateKind.Word,
            Allowed =
            [
                Entry("AFNUM", UserReportPlaceholderScope.Header),
            ],
            Excluded = Array.Empty<PlaceholderExclusion>(),
            Fingerprint = "afnum-only",
        };

        var draft = Draft("{{ds.AFNUM}}", ScanFieldScope.Header);
        var next = ScanRosterGreenMark.PreferRoster(
            [draft],
            Green("not a person id"),
            set,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        // Header-only codes stay {{ds.*}}; RewriteToRow may still flip Scope to Row when blank token rules apply.
        Assert.Equal("{{ds.AFNUM}}", Assert.Single(next).ProposedToken);
    }

    private static ScanOfficeYellowSpan Green(string text, ScanOfficeMarkKind kind = ScanOfficeMarkKind.Green) =>
        new()
        {
            Text = text,
            Region = new DocumentRegion.WordSpan("body/0", 0, text.Length),
            PageIndex = 0,
            MarkKind = kind,
        };

    private static ScanDetectedFieldDraft Draft(
        string token,
        ScanFieldScope scope,
        string? columnHeader = null) =>
        new()
        {
            FieldId = Guid.NewGuid().ToString("N"),
            Box = ScanBoundingBox.FullPage,
            PageIndex = 0,
            LabelText = "mark",
            ProposedToken = token,
            Scope = scope,
            ColumnHeader = columnHeader,
        };

    private static ApplicationProfilePlaceholderSet PlaceholderSet(params string[] codes)
    {
        var allowed = new List<UserReportPlaceholderCatalogEntry>();
        foreach (var code in codes)
        {
            var scope = code is "AFNUM" or "CHFN"
                ? UserReportPlaceholderScope.Header
                : UserReportPlaceholderScope.Both;
            if (code is "PFN" or "RNUM" or "PPN")
                scope = UserReportPlaceholderScope.Both;
            allowed.Add(Entry(code, scope));
        }

        return new ApplicationProfilePlaceholderSet
        {
            ApplicationProfileId = Guid.NewGuid(),
            DataScope = ApplicationProfileTemplateDataScope.Both,
            TemplateKind = ApplicationProfileTemplateKind.Word,
            Allowed = allowed,
            Excluded = Array.Empty<PlaceholderExclusion>(),
            Fingerprint = string.Join('-', codes),
        };
    }

    private static UserReportPlaceholderCatalogEntry Entry(string code, UserReportPlaceholderScope scope) =>
        new()
        {
            ShortCode = code,
            CanonicalPath = code,
            Scope = scope,
            RootBoTypes = Array.Empty<UserReportBoType>(),
            ExampleValue = code,
            LabelEn = code,
            LabelTk = code,
            LabelRu = code,
            LabelTr = code,
        };
}

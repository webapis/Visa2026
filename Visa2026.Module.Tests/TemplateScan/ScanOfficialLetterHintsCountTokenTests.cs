using System.Collections.Generic;
using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Count-token routing on cover letters: person vs cancel-visa/WP/invitation vs trip duration.
/// Wrong preference stamps TPCNT onto cancel/duration yellows (or the reverse).
/// </summary>
public sealed class ScanOfficialLetterHintsCountTokenTests
{
    [Fact]
    public void LooksLikeCancelVisaCount_requires_wiza_and_yatyr()
    {
        Assert.True(ScanOfficialLetterHints.LooksLikeCancelVisaCount("1 (bir) sany wizasyny ýatyrmak"));
        Assert.False(ScanOfficialLetterHints.LooksLikeCancelVisaCount("daşary ýurt raýaty"));
        Assert.False(ScanOfficialLetterHints.LooksLikeCancelVisaCount("3 aý möhletli wiza"));
    }

    [Fact]
    public void LooksLikePersonCountPhrase_rayat_and_enclosure_exclusion()
    {
        Assert.True(ScanOfficialLetterHints.LooksLikePersonCountPhrase("1 (bir) sany daşary ýurt raýaty"));
        Assert.False(ScanOfficialLetterHints.LooksLikePersonCountPhrase("Goşundy – pasport nusgalary – 1 sany"));
    }

    [Fact]
    public void LooksLikeDurationCount_gun_with_mohlet()
    {
        Assert.True(ScanOfficialLetterHints.LooksLikeDurationCount("2 (iki) gün möhlet"));
        Assert.False(ScanOfficialLetterHints.LooksLikeDurationCount("daşary ýurt raýaty"));
    }

    [Fact]
    public void ResolveCountTokenCodes_defaults_to_person_count() =>
        Assert.Equal(("TPCNT", "TPCTX"), ScanOfficialLetterHints.ResolveCountTokenCodes("1 (bir) sany daşary ýurt raýaty"));

    [Fact]
    public void ResolveCountTokenCodes_cancel_visa() =>
        Assert.Equal(
            ("CVCNT", "CVCTX"),
            ScanOfficialLetterHints.ResolveCountTokenCodes("1 (bir) sany wizasyny ýatyrmak"));

    [Fact]
    public void ResolveCountTokenCodes_cancel_work_permit() =>
        Assert.Equal(
            ("CWCNT", "CWCTX"),
            ScanOfficialLetterHints.ResolveCountTokenCodes("1 (bir) sany iş rugsatnamasyny ýatyrmak"));

    [Fact]
    public void ResolveCountTokenCodes_cancel_invitation() =>
        Assert.Equal(
            ("CICNT", "CICTX"),
            ScanOfficialLetterHints.ResolveCountTokenCodes("3 (üç) sany çakylygyny ýatyrmak"));

    [Fact]
    public void ResolveCountTokenCodes_duration() =>
        Assert.Equal(
            ("BTDCNT", "BTDCTX"),
            ScanOfficialLetterHints.ResolveCountTokenCodes("2 (iki) gün möhlet"));

    [Fact]
    public void PrefersCancelVisaCount_visa_before_person_wins()
    {
        Assert.True(ScanOfficialLetterHints.PrefersCancelVisaCount(
            "1 (bir) sany wizasyny ýatyrmak we 2 (iki) sany daşary ýurt raýaty"));
    }

    [Fact]
    public void PrefersDurationCount_when_gun_before_person()
    {
        Assert.True(ScanOfficialLetterHints.PrefersDurationCount(
            "2 (iki) gün möhlet bilen 1 (bir) sany daşary ýurt raýaty"));
    }

    [Fact]
    public void ResolveIsolatedCountTokenCodes_promotes_duration_when_TPCNT_used()
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { "TPCNT" };
        Assert.Equal(
            ("BTDCNT", "BTDCTX"),
            ScanOfficialLetterHints.ResolveIsolatedCountTokenCodes("2 gün", used));
    }

    [Fact]
    public void ResolveLetterDateTokenCode_start_end_and_application()
    {
        Assert.Equal("BTSD", ScanOfficialLetterHints.ResolveLetterDateTokenCode("-den", null));
        Assert.Equal("BTED", ScanOfficialLetterHints.ResolveLetterDateTokenCode("-ne çenli", null));
        Assert.Equal("ADAT", ScanOfficialLetterHints.ResolveLetterDateTokenCode(null, null));

        var used = new HashSet<string>(StringComparer.Ordinal) { "ADAT" };
        Assert.Equal("BTSD", ScanOfficialLetterHints.ResolveLetterDateTokenCode(null, null, used));
        used.Add("BTSD");
        Assert.Equal("BTED", ScanOfficialLetterHints.ResolveLetterDateTokenCode(null, null, used));
    }

    [Fact]
    public void LooksLikeMigrationAddressee_vs_branch_director()
    {
        Assert.True(ScanOfficialLetterHints.LooksLikeMigrationAddressee(
            "Türkmenistanyň Döwlet migrasiýa gullugynyň mudirine"));
        Assert.True(ScanOfficialLetterHints.LooksLikeBranchDirectorTitle("sahamçasynyň mudiri"));
        Assert.False(ScanOfficialLetterHints.LooksLikeBranchDirectorTitle(
            "Türkmenistanyň Döwlet migrasiýa gullugynyň mudirine"));
    }
}

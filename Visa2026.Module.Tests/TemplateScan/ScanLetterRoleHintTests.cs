using Visa2026.Module.Services.TemplateScan;
using Xunit;

namespace Visa2026.Module.Tests.TemplateScan;

/// <summary>
/// Cover-letter yellow role classification (wekil / signatory / applicant).
/// Mis-routing stamps the wrong merge token on official letters.
/// </summary>
public sealed class ScanLetterRoleHintTests
{
    [Fact]
    public void FromNearbyText_empty_is_Unknown() =>
        Assert.Equal(ScanLetterRole.Unknown, ScanLetterRoleHint.FromNearbyText(null, "  ", string.Empty));

    [Theory]
    [InlineData("Wekil")]
    [InlineData("ygtyýarly wezipeli kişi")]
    [InlineData("Authorized representative")]
    public void FromNearbyText_detects_wekil(string nearby) =>
        Assert.Equal(ScanLetterRole.Wekil, ScanLetterRoleHint.FromNearbyText(nearby));

    [Theory]
    [InlineData("Ýolbaşçy")]
    [InlineData("gol çekiji")]
    [InlineData("company head")]
    [InlineData("Mudiri")]
    [InlineData("sahamçasynyň mudiri")]
    public void FromNearbyText_detects_signatory(string nearby) =>
        Assert.Equal(ScanLetterRole.Signatory, ScanLetterRoleHint.FromNearbyText(nearby));

    [Theory]
    [InlineData("mudirine")]
    [InlineData("mudirligine")]
    public void FromNearbyText_addressee_mudir_is_not_signatory(string nearby) =>
        Assert.Equal(ScanLetterRole.Unknown, ScanLetterRoleHint.FromNearbyText(nearby));

    [Theory]
    [InlineData("doglan senesi")]
    [InlineData("atasynyň ady")]
    [InlineData("familiýasy")]
    [InlineData("çagrylan adam")]
    [InlineData("hired person")]
    [InlineData("işgär")]
    public void FromNearbyText_detects_applicant(string nearby) =>
        Assert.Equal(ScanLetterRole.Applicant, ScanLetterRoleHint.FromNearbyText(nearby));

    [Fact]
    public void FromYellowAndNearby_inline_mudiri_title_is_signatory() =>
        Assert.Equal(
            ScanLetterRole.Signatory,
            ScanLetterRoleHint.FromYellowAndNearby("Mudiri Amanow", "Ady"));

    [Fact]
    public void FromYellowAndNearby_wezipe_label_keeps_nearby_role() =>
        Assert.Equal(
            ScanLetterRole.Unknown,
            ScanLetterRoleHint.FromYellowAndNearby("Mudiri Amanow", "Wezipesi"));

    [Fact]
    public void LooksLikeWekil_true_for_representative() =>
        Assert.True(ScanLetterRoleHint.LooksLikeWekil("authorized representative"));
}

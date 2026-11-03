using System;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Banner tone/message keys for invitation later life, and visa/work-permit expiry keys.
/// </summary>
public sealed class InvitationLaterLifeBannerAndExpiryKeyTests
{
    [Theory]
    [InlineData(null, "done")]
    [InlineData("", "done")]
    [InlineData(InvitationLaterLife.InForce, "done")]
    [InlineData(InvitationLaterLife.Used, InvitationLaterLife.ToneUsed)]
    [InlineData(InvitationLaterLife.ExpiredUnused, InvitationLaterLife.ToneExpired)]
    [InlineData(InvitationLaterLife.Cancelled, InvitationLaterLife.ToneCancelled)]
    [InlineData(InvitationLaterLife.Changed, InvitationLaterLife.ToneChanged)]
    public void BannerTone_in_force_is_done_else_life_tone(string? life, string tone) =>
        Assert.Equal(tone, InvitationLaterLife.BannerTone(life));

    [Theory]
    [InlineData(InvitationLaterLife.Used, "ApplicationProfileInstance.List.Mark.InvitationUsedTitle")]
    [InlineData(InvitationLaterLife.ExpiredUnused, "ApplicationProfileInstance.List.Mark.DocumentExpiredTitle")]
    [InlineData(InvitationLaterLife.Cancelled, "ApplicationProfileInstance.List.Mark.InvitationCancelledTitle")]
    [InlineData(InvitationLaterLife.Changed, "ApplicationProfileInstance.List.Mark.InvitationChangedTitle")]
    [InlineData(InvitationLaterLife.InForce, "ApplicationProfileInstance.List.Mark.InvitationInForce")]
    [InlineData(null, "ApplicationProfileInstance.List.Mark.InvitationInForce")]
    [InlineData("unknown", "ApplicationProfileInstance.List.Mark.InvitationInForce")]
    public void BannerMessageKey_maps_life_to_catalog_key(string? life, string key) =>
        Assert.Equal(key, InvitationLaterLife.BannerMessageKey(life));

    [Fact]
    public void BannerMessageKey_resolves_to_localized_copy()
    {
        var previous = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentUICulture =
                System.Globalization.CultureInfo.GetCultureInfo("en-US");

            Assert.False(string.IsNullOrWhiteSpace(
                VisaUiMessages.Get(InvitationLaterLife.BannerMessageKey(InvitationLaterLife.Used))));
            Assert.False(string.IsNullOrWhiteSpace(
                VisaUiMessages.Get(InvitationLaterLife.BannerMessageKey(InvitationLaterLife.InForce))));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData(IssuedResultExpiry.WorkPermit, true)]
    [InlineData(IssuedResultExpiry.Visa, true)]
    [InlineData(InvitationLaterLife.ExpiredUnused, false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("visa", false)]
    public void IsExpiryKey_only_visa_and_work_permit(string? key, bool expected) =>
        Assert.Equal(expected, IssuedResultExpiry.IsExpiryKey(key));

    [Fact]
    public void ChooseSuccessor_skips_empty_document_or_application_ids()
    {
        var keptDocument = Guid.NewGuid();
        var keptApplication = Guid.NewGuid();
        var chosen = ApplicationListLinkedPeople.ChooseSuccessor(
        [
            (new DateTime(2026, 10, 1), Guid.Empty, keptApplication, "10/-1", null),
            (new DateTime(2026, 10, 2), keptDocument, Guid.Empty, "10/-2", null),
            (new DateTime(2026, 9, 1), keptDocument, keptApplication, "10/-2125", InvitationLaterLife.Changed),
        ]);

        Assert.NotNull(chosen);
        Assert.Equal(keptDocument, chosen!.DocumentId);
        Assert.Equal(keptApplication, chosen.ApplicationId);
        Assert.Equal("10/-2125", chosen.ApplicationNumber);
        Assert.Equal(InvitationLaterLife.Changed, chosen.State);
        Assert.Equal(InvitationLaterLife.ToneChanged, chosen.Tone);
    }
}

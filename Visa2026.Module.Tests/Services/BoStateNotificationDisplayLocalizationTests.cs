using Visa2026.Module.BusinessObjects.StateNotifications;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.StateNotifications;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class BoStateNotificationDisplayLocalizationTests
{
    [Fact]
    public void HandledBy_MapsKnownTokens_AndPassesThroughUnknown()
    {
        Assert.Equal(string.Empty, BoStateNotificationDisplayLocalization.HandledBy(
            new BoStateNotificationItem { HandledBy = null! }));
        Assert.Equal(string.Empty, BoStateNotificationDisplayLocalization.HandledBy(
            new BoStateNotificationItem { HandledBy = string.Empty }));

        Assert.Equal(
            VisaUiMessages.Get("StateNotification.HandledBy.StateSync"),
            BoStateNotificationDisplayLocalization.HandledBy(
                new BoStateNotificationItem { HandledBy = "State sync" }));

        Assert.Equal(
            VisaUiMessages.Get("StateNotification.HandledBy.You"),
            BoStateNotificationDisplayLocalization.HandledBy(
                new BoStateNotificationItem { HandledBy = "you" }));

        Assert.Equal(
            VisaUiMessages.Get("StateNotification.HandledBy.DemoOfficer"),
            BoStateNotificationDisplayLocalization.HandledBy(
                new BoStateNotificationItem { HandledBy = "demo.officer" }));

        Assert.Equal(
            "alice.officer",
            BoStateNotificationDisplayLocalization.HandledBy(
                new BoStateNotificationItem { HandledBy = "alice.officer" }));
    }

    [Fact]
    public void WithoutSampleKey_UsesFallbackFields_WhenCatalogMisses()
    {
        var item = new BoStateNotificationItem
        {
            BoType = "Visa",
            StateCode = "Expired",
            StateLabel = "Expired label",
            Message = "raw message",
            DisplayKey = "raw-key",
            MissingItemLabel = "Passport scan",
            SampleKey = null!,
        };

        // Catalog may or may not contain keys; assert non-throwing resolution and fallback contract.
        Assert.False(string.IsNullOrWhiteSpace(BoStateNotificationDisplayLocalization.BoType(item)));
        Assert.False(string.IsNullOrWhiteSpace(BoStateNotificationDisplayLocalization.StateLabel(item)));
        Assert.Equal("raw message", BoStateNotificationDisplayLocalization.Message(item));
        Assert.Equal("raw-key", BoStateNotificationDisplayLocalization.DisplayKey(item));
        Assert.False(string.IsNullOrWhiteSpace(BoStateNotificationDisplayLocalization.MissingItemLabel(item)));
    }

    [Fact]
    public void MissingItemLabel_FallsBackToStateLabel_WhenBlank()
    {
        var item = new BoStateNotificationItem
        {
            StateCode = "Expired",
            StateLabel = "Expired label",
            MissingItemLabel = "  ",
            SampleKey = null!,
        };

        Assert.Equal(
            BoStateNotificationDisplayLocalization.StateLabel(item),
            BoStateNotificationDisplayLocalization.MissingItemLabel(item));
    }
}

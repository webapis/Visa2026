using Visa2026.Module.Localization;
using Xunit;

namespace Visa2026.Module.Tests.Localization;

public class ApplicationItemDocumentCopiesSlotLocalizationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetLabel_BlankSlot_ReturnsEmpty(string slotKey)
    {
        Assert.Equal(string.Empty, ApplicationItemDocumentCopiesSlotLocalization.GetLabel(slotKey));
    }

    [Theory]
    [InlineData("Passport.Current", "ApplicationItemDocumentCopies.Slot.Passport.Current")]
    [InlineData("Visa.Next", "ApplicationItemDocumentCopies.Slot.Visa.Next")]
    [InlineData("Education.Current", "ApplicationItemDocumentCopies.Slot.Education.Current")]
    [InlineData("FamilyRelationship.Current", "ApplicationItemDocumentCopies.Slot.FamilyRelationship")]
    [InlineData("AddressOfResidence.Lodging", "ApplicationItemDocumentCopies.Slot.Address.Lodging")]
    public void GetLabel_KnownSlot_ResolvesMessageKey(string slotKey, string messageKey)
    {
        var label = ApplicationItemDocumentCopiesSlotLocalization.GetLabel(slotKey, "en-US");

        Assert.Equal(VisaUiMessages.Get(messageKey, "en-US"), label);
        Assert.False(string.IsNullOrWhiteSpace(label));
        Assert.NotEqual(slotKey, label);
    }

    [Fact]
    public void GetLabel_UnknownSlot_ReturnsSlotKey()
    {
        const string unknown = "Custom.UnknownSlot";

        Assert.Equal(unknown, ApplicationItemDocumentCopiesSlotLocalization.GetLabel(unknown));
    }
}

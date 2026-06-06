namespace Visa2026.Module.Localization;

/// <summary>Maps document-copy slot keys to localized labels (culture-aware).</summary>
public static class ApplicationItemDocumentCopiesSlotLocalization
{
    private static readonly Dictionary<string, string> SlotMessageKeys = new(StringComparer.Ordinal)
    {
        ["Passport.Current"] = "ApplicationItemDocumentCopies.Slot.Passport.Current",
        ["Passport.Previous"] = "ApplicationItemDocumentCopies.Slot.Passport.Previous",
        ["Visa.Current"] = "ApplicationItemDocumentCopies.Slot.Visa.Current",
        ["Visa.Next"] = "ApplicationItemDocumentCopies.Slot.Visa.Next",
        ["WorkPermit.Current"] = "ApplicationItemDocumentCopies.Slot.WorkPermit.Current",
        ["WorkPermit.Previous"] = "ApplicationItemDocumentCopies.Slot.WorkPermit.Previous",
        ["Education.Current"] = "ApplicationItemDocumentCopies.Slot.Education.Current",
        ["Invitation.Current"] = "ApplicationItemDocumentCopies.Slot.Invitation.Current",
        ["Invitation.Previous"] = "ApplicationItemDocumentCopies.Slot.Invitation.Previous",
        ["AddressOfResidence.Current"] = "ApplicationItemDocumentCopies.Slot.Address.Current",
        ["AddressOfResidence.Lodging"] = "ApplicationItemDocumentCopies.Slot.Address.Lodging",
        ["MedicalRecord.Current"] = "ApplicationItemDocumentCopies.Slot.MedicalRecord.Current",
        ["FamilyRelationship.Current"] = "ApplicationItemDocumentCopies.Slot.FamilyRelationship",
    };

    public static string GetLabel(string slotKey, string? cultureName = null)
    {
        if (string.IsNullOrWhiteSpace(slotKey))
        {
            return string.Empty;
        }

        return SlotMessageKeys.TryGetValue(slotKey, out string? messageKey)
            ? VisaUiMessages.Get(messageKey, cultureName)
            : slotKey;
    }
}

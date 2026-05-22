namespace Visa2026.Module.Localization;

/// <summary>Localized strings for <c>PACKAGING_NOTES.txt</c> and <see cref="BusinessObjects.PdfGenerationBatch.PdfPackagingNotes"/>.</summary>
public static class PdfPackagingNotesLocalization
{
    public static string SlotLabel(string culture, string currentOrPrevious) =>
        string.Equals(currentOrPrevious, "Current", StringComparison.OrdinalIgnoreCase)
            ? VisaUiMessages.Get("Pdf.Packaging.Slot.Current", culture)
            : VisaUiMessages.Get("Pdf.Packaging.Slot.Previous", culture);

    public static string FormatGap(string culture, string messageKey, params object[] args) =>
        VisaUiMessages.FormatForCulture(culture, messageKey, args);
}

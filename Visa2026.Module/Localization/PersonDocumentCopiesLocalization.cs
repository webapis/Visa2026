namespace Visa2026.Module.Localization;

/// <summary>Person document copies record labels and formatting helpers.</summary>
public static class PersonDocumentCopiesLocalization
{
    public static string FormatPassportRecord(string? passportNumber) =>
        FormatRecord("PersonDocumentCopies.Record.Passport", passportNumber);

    public static string FormatVisaRecord(string? visaNumber) =>
        FormatRecord("PersonDocumentCopies.Record.Visa", visaNumber);

    public static string FormatEducationRecord(string? caption) =>
        FormatRecord("PersonDocumentCopies.Record.Education", caption);

    public static string FormatMedicalRecord(string? documentNumber) =>
        FormatRecord("PersonDocumentCopies.Record.MedicalRecord", documentNumber);

    public static string FormatAddressRecord(string? address) =>
        FormatRecord("PersonDocumentCopies.Record.Address", address);

    public static string FormatLodgingRecord(string? address) =>
        FormatRecord("PersonDocumentCopies.Record.Lodging", address);

    public static string FormatWorkPermitRecord(string? number) =>
        FormatRecord("PersonDocumentCopies.Record.WorkPermit", number);

    public static string FormatInvitationRecord(string? number) =>
        FormatRecord("PersonDocumentCopies.Record.Invitation", number);

    public static string FormatBorderZoneRecord(string? number) =>
        FormatRecord("PersonDocumentCopies.Record.BorderZone", number);

    public static string FormatRejectionRecord(string? caption) =>
        FormatRecord("PersonDocumentCopies.Record.Rejection", caption);

    public static string FormatPersonDocumentRecord(string? fileName) =>
        FormatRecord("PersonDocumentCopies.Record.PersonDocument", fileName);

    public static string FormatFamilyRelationDocumentRecord(string? fileName) =>
        FormatRecord("PersonDocumentCopies.Record.FamilyRelationDocument", fileName);

    public static string CurrentBadge(string? cultureName = null) =>
        VisaUiMessages.Get("PersonDocumentCopies.Badge.Current", cultureName);

    private static string FormatRecord(string messageKey, string? caption) =>
        VisaUiMessages.Format(messageKey, string.IsNullOrWhiteSpace(caption) ? "—" : caption.Trim());
}

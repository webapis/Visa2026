using System;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public static class ApplicationItemDocumentCopiesPackageSlotRules
{
    public static bool IsSlotIncludedInPackage(string slotKey, ApplicationItemDocumentPackageOptions options)
    {
        if (options == null || string.IsNullOrWhiteSpace(slotKey))
            return false;

        if (slotKey.StartsWith("Passport.", StringComparison.Ordinal))
            return options.IncludePassportCopies;

        if (slotKey.StartsWith("Visa.", StringComparison.Ordinal))
            return options.IncludeVisaCopies;

        if (slotKey.StartsWith("WorkPermit.", StringComparison.Ordinal))
            return options.IncludeWorkPermitCopies;

        if (slotKey.StartsWith("Education.", StringComparison.Ordinal))
            return options.IncludeDiplomaFiles;

        if (slotKey.StartsWith("AddressOfResidence.", StringComparison.Ordinal))
            return options.IncludeAddressOfResidenceCopies;

        if (slotKey.StartsWith("MedicalRecord.", StringComparison.Ordinal))
            return options.IncludeMedicalRecordCopies;

        if (slotKey.StartsWith("Invitation.", StringComparison.Ordinal))
            return options.IncludeInvitationCopies;

        if (slotKey.StartsWith("FamilyRelationship.", StringComparison.Ordinal))
            return options.IncludeFamilyRelationshipCopies;

        return false;
    }
}

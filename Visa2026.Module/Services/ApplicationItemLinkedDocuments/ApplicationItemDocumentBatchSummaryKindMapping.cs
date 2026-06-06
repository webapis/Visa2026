using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Module.Services.ApplicationItemLinkedDocuments;

public enum ApplicationItemDocumentBatchSummaryKind
{
    CurrentPassports,
    CurrentVisas,
    CurrentWorkPermits,
    AllDiplomas
}

public static class ApplicationItemDocumentBatchSummaryKindMapping
{
    public static bool TryFromSlotKey(
        string slotKey,
        ApplicationItemDocumentPackageOptions options,
        out ApplicationItemDocumentBatchSummaryKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(slotKey) || options == null)
            return false;

        if (options.SupportingZipMergeOption == PdfSupportingZipMergeOption.IndividualFilesOnly)
            return false;

        if (string.Equals(slotKey, "Passport.Current", StringComparison.Ordinal) && options.IncludePassportCopies)
        {
            kind = ApplicationItemDocumentBatchSummaryKind.CurrentPassports;
            return true;
        }

        if (string.Equals(slotKey, "Visa.Current", StringComparison.Ordinal) && options.IncludeVisaCopies)
        {
            kind = ApplicationItemDocumentBatchSummaryKind.CurrentVisas;
            return true;
        }

        if (string.Equals(slotKey, "WorkPermit.Current", StringComparison.Ordinal) && options.IncludeWorkPermitCopies)
        {
            kind = ApplicationItemDocumentBatchSummaryKind.CurrentWorkPermits;
            return true;
        }

        if (string.Equals(slotKey, "Education.Current", StringComparison.Ordinal)
            && options.IncludeDiplomaFiles
            && options.DiplomaScope == PdfBatchDiplomaScope.AllEducations)
        {
            kind = ApplicationItemDocumentBatchSummaryKind.AllDiplomas;
            return true;
        }

        return false;
    }

    public static string GetDownloadFileName(ApplicationItemDocumentBatchSummaryKind kind) =>
        kind switch
        {
            ApplicationItemDocumentBatchSummaryKind.CurrentPassports => "CurrentPassports.pdf",
            ApplicationItemDocumentBatchSummaryKind.CurrentVisas => "CurrentVisas.pdf",
            ApplicationItemDocumentBatchSummaryKind.CurrentWorkPermits => "CurrentWorkPermits.pdf",
            ApplicationItemDocumentBatchSummaryKind.AllDiplomas => "AllDiplomas.pdf",
            _ => "summary.pdf"
        };
}

using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Via-ministry step after the last approval leg: documents are at the migration
/// service, and the CO (AS) number has not been received.
/// </summary>
public static class ApplicationMigrationSubmission
{
    public const string TimelineKey = "migration-submitted";
    public const string TimelineLabel = "Submitted to migration service";
    public const int MaxDocumentNumberLength = 100;

    public static bool IsState(string? stateCode) =>
        !string.IsNullOrWhiteSpace(stateCode)
        && string.Equals(
            stateCode.Trim(),
            ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration,
            StringComparison.OrdinalIgnoreCase);

    public static bool TryNormalizeDocumentNumber(string? value, out string normalized, out string? error)
    {
        normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberRequired");
            return false;
        }

        if (normalized.Length > MaxDocumentNumberLength)
        {
            normalized = string.Empty;
            error = VisaUiMessages.Get("ApplicationProfileInstance.Workspace.SubmissionDocumentNumberTooLong");
            return false;
        }

        error = null;
        return true;
    }

    public static bool HasDocumentCopy(ApplicationProfileInstanceProgress? row) =>
        row?.SubmissionDocumentFile != null && row.SubmissionDocumentFile.Size > 0;

    public static bool IsComplete(ApplicationProfileInstanceProgress? row) =>
        IsState(row?.State?.Code)
        && row!.Date != default
        && !string.IsNullOrWhiteSpace(row.SubmissionDocumentNumber)
        && HasDocumentCopy(row);
}

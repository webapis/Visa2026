using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

/// <summary>
/// Notes while <see cref="ApplicationProfileInstance"/> has no progress row (implied office).
/// </summary>
internal static class ApplicationProfileInstanceOfficeNotesHelper
{
    public static void Save(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress? latest,
        string? notes)
    {
        var text = notes?.Trim() ?? string.Empty;
        if (latest != null)
            latest.Description = text;
        else
            application.OfficePreparationNotes = text;
    }

    public static void CopyOntoNewRow(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress newRow,
        string? notesOnLatestStep)
    {
        var text = notesOnLatestStep?.Trim();
        if (string.IsNullOrEmpty(text))
            text = application.OfficePreparationNotes?.Trim();

        if (!string.IsNullOrEmpty(text))
            newRow.Description = text;

        application.OfficePreparationNotes = null;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

public readonly record struct ApplicationProfileInstanceProgressStoredFile(Guid FileId, string FileName);

/// <summary>
/// Files on one progress step. The first file remains the existing single
/// <see cref="FileData"/> column. Further files are <see cref="ApplicationProfileInstanceProgressFile"/> rows.
/// </summary>
public static class ApplicationProfileInstanceProgressFiles
{
    public const int MinistryLetter = 0;
    public const int SubmissionDocument = 1;

    public static IReadOnlyList<ApplicationProfileInstanceProgressStoredFile> List(
        ApplicationProfileInstanceProgress? row,
        int slot)
    {
        if (row == null)
            return Array.Empty<ApplicationProfileInstanceProgressStoredFile>();

        var files = new List<ApplicationProfileInstanceProgressStoredFile>();
        var primary = PrimaryFile(row, slot);
        if (primary != null && primary.ID != Guid.Empty && primary.Size > 0)
        {
            files.Add(new ApplicationProfileInstanceProgressStoredFile(
                primary.ID,
                string.IsNullOrWhiteSpace(primary.FileName) ? "file" : primary.FileName));
        }

        if (row.Files == null)
            return files;

        foreach (var extra in row.Files
                     .Where(item => item != null && item.Slot == slot && item.File != null && item.File.Size > 0 && item.File.ID != Guid.Empty)
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => item.ID))
        {
            if (primary != null && extra.File.ID == primary.ID)
                continue;

            files.Add(new ApplicationProfileInstanceProgressStoredFile(
                extra.File.ID,
                string.IsNullOrWhiteSpace(extra.File.FileName) ? "file" : extra.File.FileName));
        }

        return files;
    }

    public static FileData? PrimaryFile(ApplicationProfileInstanceProgress row, int slot) =>
        slot == SubmissionDocument ? row.SubmissionDocumentFile : row.MinistryLetterFile;

    public static void AssignPrimary(ApplicationProfileInstanceProgress row, int slot, FileData? file)
    {
        if (slot == SubmissionDocument)
            row.SubmissionDocumentFile = file;
        else
            row.MinistryLetterFile = file;
    }
}

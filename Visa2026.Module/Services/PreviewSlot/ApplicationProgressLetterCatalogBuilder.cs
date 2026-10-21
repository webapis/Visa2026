using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PreviewSlot;

public static class ApplicationProfileInstanceProgressLetterCatalogBuilder
{
    public static IReadOnlyList<ApplicationProfileInstanceProgressLetterCatalogEntry> Build(
        IObjectSpace objectSpace,
        Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return Array.Empty<ApplicationProfileInstanceProgressLetterCatalogEntry>();

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstanceProgress>()
            .Include(p => p.ApplicationProfileInstance)
            .Include(p => p.State)
            .Include(p => p.MinistryLetterFile)
            .Include(p => p.SubmissionDocumentFile)
            .Include(p => p.Files)
                .ThenInclude(f => f.File)
            .Where(p => p.ApplicationProfileInstance != null && p.ApplicationProfileInstance.ID == applicationId)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .ToList();

        var entries = new List<ApplicationProfileInstanceProgressLetterCatalogEntry>();
        foreach (var row in rows)
        {
            AddEntries(entries, row, ApplicationProfileInstanceProgressFiles.MinistryLetter);
            AddEntries(entries, row, ApplicationProfileInstanceProgressFiles.SubmissionDocument);
        }

        return entries;
    }

    private static void AddEntries(
        List<ApplicationProfileInstanceProgressLetterCatalogEntry> entries,
        ApplicationProfileInstanceProgress row,
        int slot)
    {
        foreach (var file in ApplicationProfileInstanceProgressFiles.List(row, slot))
        {
            entries.Add(new ApplicationProfileInstanceProgressLetterCatalogEntry
            {
                ProgressId = row.ID,
                StatusLabel = row.StatusListLabel,
                Date = row.Date,
                FileName = file.FileName,
                FileId = file.FileId,
            });
        }
    }
}

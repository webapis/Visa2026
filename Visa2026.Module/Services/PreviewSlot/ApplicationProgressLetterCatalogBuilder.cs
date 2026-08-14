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

        return objectSpace.GetObjectsQuery<ApplicationProfileInstanceProgress>()
            .Include(p => p.ApplicationProfileInstance)
            .Include(p => p.State)
            .Include(p => p.MinistryLetterFile)
            .Where(p => p.ApplicationProfileInstance != null && p.ApplicationProfileInstance.ID == applicationId)
            .Where(p => p.MinistryLetterFile != null && p.MinistryLetterFile.Size > 0)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .ToList()
            .Select(p => new ApplicationProfileInstanceProgressLetterCatalogEntry
            {
                ProgressId = p.ID,
                StatusLabel = p.StatusListLabel,
                Date = p.Date,
                FileName = p.MinistryLetterFileName,
            })
            .ToList();
    }
}

using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PreviewSlot;

public static class ApplicationProgressLetterCatalogBuilder
{
    public static IReadOnlyList<ApplicationProgressLetterCatalogEntry> Build(
        IObjectSpace objectSpace,
        Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return Array.Empty<ApplicationProgressLetterCatalogEntry>();

        return objectSpace.GetObjectsQuery<ApplicationProgress>()
            .Include(p => p.Application)
            .Include(p => p.State)
            .Include(p => p.Location)
            .Include(p => p.MinistryLetterFile)
            .Where(p => p.Application != null && p.Application.ID == applicationId)
            .Where(p => p.MinistryLetterFile != null && p.MinistryLetterFile.Size > 0)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .ToList()
            .Select(p => new ApplicationProgressLetterCatalogEntry
            {
                ProgressId = p.ID,
                StatusLabel = p.StatusListLabel,
                Date = p.Date,
                FileName = p.MinistryLetterFileName,
            })
            .ToList();
    }
}

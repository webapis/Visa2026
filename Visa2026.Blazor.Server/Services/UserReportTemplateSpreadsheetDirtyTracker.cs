using System.Collections.Concurrent;

namespace Visa2026.Blazor.Server.Services;

/// <summary>Tracks unsaved Spreadsheet iframe edits per template (DetailView close guard).</summary>
public sealed class UserReportTemplateSpreadsheetDirtyTracker
{
    private readonly ConcurrentDictionary<Guid, bool> _dirty = new();

    public void SetDirty(Guid templateId, bool isDirty)
    {
        if (templateId == Guid.Empty)
            return;

        if (isDirty)
            _dirty[templateId] = true;
        else
            _dirty.TryRemove(templateId, out _);
    }

    public bool IsDirty(Guid templateId) =>
        templateId != Guid.Empty
        && _dirty.TryGetValue(templateId, out var dirty)
        && dirty;
}

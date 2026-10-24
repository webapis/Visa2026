using System;
using System.Collections.Concurrent;

namespace Visa2026.Blazor.Server.Services;

/// <summary>
/// Circuit-scoped cache of generated Report Dashboard Excel workbooks for File-occupant preview.
/// </summary>
public interface IReportDashboardExcelPreviewStore
{
    Guid Put(byte[] xlsxContent, string fileName, string displayName);

    bool TryGet(Guid id, out ReportDashboardExcelPreviewEntry entry);
}

public sealed class ReportDashboardExcelPreviewEntry
{
    public required byte[] XlsxContent { get; init; }

    public required string FileName { get; init; }

    public required string DisplayName { get; init; }
}

public sealed class ReportDashboardExcelPreviewStore : IReportDashboardExcelPreviewStore
{
    private readonly ConcurrentDictionary<Guid, ReportDashboardExcelPreviewEntry> _entries = new();

    public Guid Put(byte[] xlsxContent, string fileName, string displayName)
    {
        ArgumentNullException.ThrowIfNull(xlsxContent);
        if (xlsxContent.Length == 0)
            throw new ArgumentException("Workbook is empty.", nameof(xlsxContent));

        var id = Guid.NewGuid();
        _entries[id] = new ReportDashboardExcelPreviewEntry
        {
            XlsxContent = xlsxContent,
            FileName = string.IsNullOrWhiteSpace(fileName) ? "report.xlsx" : fileName.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Report" : displayName.Trim(),
        };
        return id;
    }

    public bool TryGet(Guid id, out ReportDashboardExcelPreviewEntry entry)
    {
        if (id != Guid.Empty && _entries.TryGetValue(id, out var found) && found != null)
        {
            entry = found;
            return true;
        }

        entry = null!;
        return false;
    }
}
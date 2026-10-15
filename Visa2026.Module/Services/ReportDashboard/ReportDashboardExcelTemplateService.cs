using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Npgsql;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// One company workbook per invitation subreport. The file keeps the administrator's
/// title, header labels, fonts, colors, and column widths. Export writes the rows
/// the caller passes into those fixed columns.
/// </summary>
public static class ReportDashboardExcelTemplateService
{
    public const int MaxBytes = 8 * 1024 * 1024;

    public const string SavedKey = "ReportDashboard.Chrome.TemplateSaved";
    public const string NotExcelKey = "ReportDashboard.Chrome.TemplateNotExcel";
    public const string ColumnsKey = "ReportDashboard.Chrome.TemplateColumns";
    public const string AdminOnlyKey = "ReportDashboard.Chrome.TemplateAdminOnly";

    public static bool Supports(string? subReport) => CanonicalKey(subReport) != null;

    /// <summary>Visible invitation tab this workbook belongs to. Hidden (V) twins share it.</summary>
    public static string? CanonicalKey(string? subReport) => subReport switch
    {
        "ready-by-project" or "ready-by-period-category" => "ready-by-project",
        "expired-invitation" => "expired-invitation",
        "cancelled-invitation" => "cancelled-invitation",
        "in-process" or "in-process-by-period-category-type" => "in-process",
        "process-result" or "process-result-by-period-category-type"
            or "rejected-by-project" or "rejected-by-period-category-type" => "process-result",
        "used" or "used-by-period-category-type" => "used",
        "valid-until" or "expired" => "valid-until",
        _ => null
    };

    public static byte[] Export(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string? subReport,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        bool includeChartColumn)
    {
        var template = LoadContent(objectSpace, category, subReport);
        return ReportDashboardExcelTemplateWorkbook.Export(
            template, title, headers, subReport, rows, includeChartColumn);
    }

    public static byte[] DownloadTemplate(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string? subReport,
        string title,
        IReadOnlyList<string> headers)
    {
        var template = LoadContent(objectSpace, category, subReport);
        if (template != null
            && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, headers.Count))
        {
            return template;
        }

        return ReportDashboardExcelTemplateWorkbook.BuildStarter(title, headers);
    }

    /// <summary>Returns a localization key. <see cref="SavedKey"/> means the file was stored.</summary>
    public static string Save(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string? subReport,
        string? fileName,
        byte[] content,
        int expectedColumns)
    {
        if (!ApplicationRuntimeLogAdminHelper.IsCurrentUserAdministrator())
            return AdminOnlyKey;

        var key = CanonicalKey(subReport);
        if (key == null)
            return NotExcelKey;

        if (content.Length == 0 || content.Length > MaxBytes
            || fileName == null
            || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return NotExcelKey;
        }

        if (!ReportDashboardExcelTemplateWorkbook.MatchesColumns(content, expectedColumns))
            return ColumnsKey;

        ReportDashboardExcelTemplate? row;
        try
        {
            row = objectSpace.GetObjectsQuery<ReportDashboardExcelTemplate>()
                .FirstOrDefault(t => t.Category == category && t.SubReportKey == key);
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return NotExcelKey;
        }

        if (row == null)
        {
            row = objectSpace.CreateObject<ReportDashboardExcelTemplate>();
            row.Category = category;
            row.SubReportKey = key;
        }

        if (row.TemplateFile == null)
            row.TemplateFile = objectSpace.CreateObject<FileData>();

        row.TemplateFile.FileName = string.IsNullOrWhiteSpace(fileName) ? key + ".xlsx" : fileName;
        row.TemplateFile.Content = content;
        row.TemplateFile.Size = content.Length;
        objectSpace.CommitChanges();
        return SavedKey;
    }

    private static byte[]? LoadContent(
        IObjectSpace objectSpace, ReportDashboardCategory category, string? subReport)
    {
        var key = CanonicalKey(subReport);
        if (key == null)
            return null;

        try
        {
            var row = objectSpace.GetObjectsQuery<ReportDashboardExcelTemplate>()
                .FirstOrDefault(t => t.Category == category && t.SubReportKey == key);
            var content = row?.TemplateFile?.Content;
            return content is { Length: > 0 } ? content : null;
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return null;
        }
    }

    private static bool IsMissingTable(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == PostgresErrorCodes.UndefinedTable)
                return true;
        }

        return false;
    }
}

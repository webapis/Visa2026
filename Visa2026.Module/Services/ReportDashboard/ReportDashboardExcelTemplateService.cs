using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Npgsql;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.RuntimeLogging;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// One company workbook per officer-table subreport. The file keeps the administrator's
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

    public static bool Supports(ReportDashboardCategory category, string? subReport) =>
        CanonicalKey(category, subReport) != null;

    /// <summary>Stored key for this tab. Invitation (V) twins share the (P) key. A Maglumat is Work Permit only.</summary>
    public static string? CanonicalKey(ReportDashboardCategory category, string? subReport)
    {
        if (category == ReportDashboardCategory.VisaExtension
            && string.Equals(subReport, ReportDashboardCatalog.VisaUzatilanlarKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.VisaUzatilanlarKey;

        if (category == ReportDashboardCategory.VisaExtension
            && string.Equals(subReport, ReportDashboardCatalog.VisaAktivPersonellerKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.VisaAktivPersonellerKey;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitKotaOranimizKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitKotaOranimizKey;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitPersonelSayiKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitPersonelSayiKey;

        if (category == ReportDashboardCategory.Registration
            && string.Equals(subReport, ReportDashboardCatalog.RegistrationOturumSayisiKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.RegistrationOturumSayisiKey;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitSanawKey, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitSanawKey;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat3Key, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitMaglumat3Key;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat2Key, StringComparison.OrdinalIgnoreCase))
            return ReportDashboardCatalog.WorkPermitMaglumat2Key;

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase))
            return "active-by-project";

        if (category != ReportDashboardCategory.Invitation)
            return null;

        return subReport switch
        {
            "ready-by-project" or "ready-by-period-category" => "ready-by-project",
            "expired-invitation" => "expired-invitation",
            "cancelled-invitation" => "cancelled-invitation",
            "in-process" or "in-process-by-period-category-type" => "in-process",
            ReportDashboardCatalog.InvitationInProcessLongKey => ReportDashboardCatalog.InvitationInProcessLongKey,
            "process-result" or "process-result-by-period-category-type"
                or "rejected-by-project" or "rejected-by-period-category-type" => "process-result",
            "used" or "used-by-period-category-type" => "used",
            "valid-until" or "expired" => "valid-until",
            _ => null
        };
    }

    public static byte[] Export(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string? subReport,
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<ReportDashboardPreviewRow> rows,
        bool includeChartColumn)
    {
        byte[] content;
        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitKotaOranimizKey, StringComparison.OrdinalIgnoreCase))
            content = ExportKotaOranimiz(objectSpace, rows);
        else if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitPersonelSayiKey, StringComparison.OrdinalIgnoreCase))
            content = ExportPersonelSayi(objectSpace, rows);
        else if (category == ReportDashboardCategory.Registration
            && string.Equals(subReport, ReportDashboardCatalog.RegistrationOturumSayisiKey, StringComparison.OrdinalIgnoreCase))
            content = ExportOturumSayisi(objectSpace, rows);
        else
        {
            var template = LoadContent(objectSpace, category, subReport);
            content = ReportDashboardExcelTemplateWorkbook.Export(
                template, title, headers, category, subReport, rows, includeChartColumn);
        }

        return ReportDashboardExcelPrintLayout.Apply(content, category, subReport);
    }

    private static byte[] ExportPersonelSayi(
        IObjectSpace objectSpace,
        IReadOnlyList<ReportDashboardPreviewRow> rows)
    {
        var template = LoadContent(objectSpace, ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitPersonelSayiKey);
        var data = rows
            .Select(row =>
            {
                _ = int.TryParse(row.ColumnA, out var active);
                _ = int.TryParse(row.ColumnB, out var inactive);
                return (Department: row.Name ?? string.Empty, Active: active, Inactive: inactive);
            })
            .ToList();
        return ReportDashboardWorkPermitPersonelSayiExcel.Fill(template, data);
    }

    private static byte[] ExportOturumSayisi(
        IObjectSpace objectSpace,
        IReadOnlyList<ReportDashboardPreviewRow> rows)
    {
        var template = LoadContent(objectSpace, ReportDashboardCategory.Registration, ReportDashboardCatalog.RegistrationOturumSayisiKey);
        static int ParseCount(string? text) =>
            int.TryParse(text, out var value) ? value : 0;

        var data = rows
            .Select(row => new ReportDashboardRegistrationOturumSayisiExcel.Row(
                row.Name ?? string.Empty,
                ParseCount(row.ColumnA),
                ParseCount(row.ColumnB),
                ParseCount(row.ColumnC),
                0,
                0,
                ParseCount(row.ColumnD),
                0,
                0,
                ParseCount(row.ColumnE),
                0,
                0,
                ParseCount(row.ColumnF),
                ParseCount(row.ColumnG),
                row.ColumnI ?? string.Empty))
            .ToList();
        return ReportDashboardRegistrationOturumSayisiExcel.Fill(template, data);
    }

    private static byte[] ExportKotaOranimiz(
        IObjectSpace objectSpace,
        IReadOnlyList<ReportDashboardPreviewRow> rows)
    {
        var template = LoadContent(objectSpace, ReportDashboardCategory.WorkPermit, ReportDashboardCatalog.WorkPermitKotaOranimizKey)
            ?? ReportDashboardWorkPermitKotaExcel.BuildStarter();
        var settings = WorkPermitQuotaSettings.GetOrCreateInstance(objectSpace);
        var row = rows.FirstOrDefault();
        var reportDate = DateTime.Today;
        if (row?.ColumnD is { Length: > 0 } dateText
            && DateTime.TryParseExact(dateText, "dd.MM.yyyy", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            reportDate = parsed.Date;
        var foreign = 0;
        var inProcess = 0;
        if (row != null)
        {
            _ = int.TryParse(row.ColumnA, out foreign);
            _ = int.TryParse(row.ColumnB, out inProcess);
        }

        return ReportDashboardWorkPermitKotaExcel.Fill(
            template,
            reportDate,
            settings.LocalPersonnelCount,
            foreign,
            inProcess,
            settings.ForeignRatio > 0 ? settings.ForeignRatio : WorkPermitQuotaSettings.DefaultForeignRatio,
            settings.LocalRatio > 0 ? settings.LocalRatio : WorkPermitQuotaSettings.DefaultLocalRatio);
    }

    public static byte[] DownloadTemplate(
        IObjectSpace objectSpace,
        ReportDashboardCategory category,
        string? subReport,
        string title,
        IReadOnlyList<string> headers)
    {
        var template = LoadContent(objectSpace, category, subReport);
        if (ReportDashboardExcelTemplateWorkbook.IsInvitationCompanySheet(category, subReport)
            && !string.Equals(subReport, "ready-by-period-category", StringComparison.Ordinal))
        {
            return FinishDownload(category, subReport, template != null
                && ReportDashboardExcelTemplateWorkbook.MatchesColumns(
                    template, ReportDashboardInvitationTableExcel.CompanyHeaders.Length)
                ? ReportDashboardExcelTemplateWorkbook.ApplyInvitationCompanyHeaders(template)
                : ReportDashboardExcelTemplateWorkbook.BuildInvitationCompanyStarter());
        }

        if (ReportDashboardExcelTemplateWorkbook.IsInvitationProcessCompanySheet(category, subReport))
        {
            var longTerm = ReportDashboardCatalog.IsInvitationProcessLongSubReport(subReport);
            var processHeaders = longTerm
                ? ReportDashboardInvitationTableExcel.ProcessCompanyLongHeaders
                : ReportDashboardInvitationTableExcel.ProcessCompanyHeaders;
            return FinishDownload(category, subReport, template != null
                && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, processHeaders.Length)
                ? ReportDashboardExcelTemplateWorkbook.ApplyInvitationProcessCompanyHeaders(template, longTerm)
                : (longTerm
                    ? ReportDashboardExcelTemplateWorkbook.BuildInvitationProcessLongCompanyStarter()
                    : ReportDashboardExcelTemplateWorkbook.BuildInvitationProcessCompanyStarter()));
        }

        if (category == ReportDashboardCategory.VisaExtension
            && string.Equals(subReport, ReportDashboardCatalog.VisaUzatilanlarKey, StringComparison.OrdinalIgnoreCase))
        {
            var uzatilanlar = template != null
                && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, headers.Count)
                ? template
                : ReportDashboardExcelTemplateWorkbook.BuildUzatilanlarStarter();
            return FinishDownload(category, subReport, uzatilanlar, headers);
        }

        if (category == ReportDashboardCategory.VisaExtension
            && string.Equals(subReport, ReportDashboardCatalog.VisaAktivPersonellerKey, StringComparison.OrdinalIgnoreCase))
        {
            var aktiv = template != null
                && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, headers.Count)
                ? template
                : ReportDashboardExcelTemplateWorkbook.BuildCalismaListesiStarter("aktiv personeller");
            return FinishDownload(category, subReport, aktiv, headers);
        }

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey, StringComparison.OrdinalIgnoreCase))
        {
            var calisma = template != null
                && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, headers.Count)
                ? template
                : ReportDashboardExcelTemplateWorkbook.BuildCalismaListesiStarter();
            return FinishDownload(category, subReport, calisma, headers);
        }

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitKotaOranimizKey, StringComparison.OrdinalIgnoreCase))
        {
            return FinishDownload(category, subReport, ReportDashboardWorkPermitKotaExcel.BuildStarter(), headers);
        }

        if (category == ReportDashboardCategory.WorkPermit
            && string.Equals(subReport, ReportDashboardCatalog.WorkPermitPersonelSayiKey, StringComparison.OrdinalIgnoreCase))
        {
            return FinishDownload(category, subReport, ReportDashboardWorkPermitPersonelSayiExcel.BuildStarter(), headers);
        }

        if (category == ReportDashboardCategory.Registration
            && string.Equals(subReport, ReportDashboardCatalog.RegistrationOturumSayisiKey, StringComparison.OrdinalIgnoreCase))
        {
            return FinishDownload(category, subReport, ReportDashboardRegistrationOturumSayisiExcel.BuildStarter(), headers);
        }

        if (template != null
            && ReportDashboardExcelTemplateWorkbook.MatchesColumns(template, headers.Count))
        {
            return FinishDownload(category, subReport, template, headers);
        }

        return FinishDownload(
            category,
            subReport,
            ReportDashboardExcelTemplateWorkbook.BuildStarter(
                title, headers, SheetName(category, subReport),
                category is ReportDashboardCategory.Invitation or ReportDashboardCategory.WorkPermit),
            headers);
    }

    private static byte[] FinishDownload(
        ReportDashboardCategory category,
        string? subReport,
        byte[] content,
        IReadOnlyList<string>? headerLabels = null)
    {
        var projectSheet = headerLabels != null
            && headerLabels.Count == ReportDashboardWorkPermitCalismaExcel.Headers.Length
            && string.Equals(headerLabels[1], "PROJECT", StringComparison.Ordinal);
        var uzatilanlarSheet = headerLabels != null
            && headerLabels.Count == ReportDashboardVisaUzatilanlarExcel.Headers.Length
            && string.Equals(headerLabels[5], "Mevcut Vize bitiş tarihi", StringComparison.Ordinal);
        if (projectSheet
            || uzatilanlarSheet
            || (category == ReportDashboardCategory.WorkPermit
                && headerLabels != null
                && string.Equals(
                    headerLabels.Count > 10 ? headerLabels[10] : null,
                    "Möhleti we gezekligi",
                    StringComparison.Ordinal)))
        {
            content = ReportDashboardExcelTemplateWorkbook.ApplyHeaderLabels(content, headerLabels);
        }

        if (category is ReportDashboardCategory.Invitation or ReportDashboardCategory.WorkPermit
            || projectSheet
            || uzatilanlarSheet)
        {
            content = ReportDashboardExcelTemplateWorkbook.WithCompanyFont(
                content,
                ReportDashboardExcelTemplateWorkbook.MaglumatColumnWidths(headerLabels));
        }

        return ReportDashboardExcelPrintLayout.Apply(content, category, subReport);
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

        var key = CanonicalKey(category, subReport);
        if (key == null)
            return NotExcelKey;

        if (content.Length == 0 || content.Length > MaxBytes
            || fileName == null
            || !fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return NotExcelKey;
        }

        if (ReportDashboardExcelTemplateWorkbook.IsInvitationCompanySheet(category, subReport)
            && !string.Equals(subReport, "ready-by-period-category", StringComparison.Ordinal))
        {
            expectedColumns = ReportDashboardInvitationTableExcel.CompanyHeaders.Length;
        }

        if (ReportDashboardExcelTemplateWorkbook.IsInvitationProcessCompanySheet(category, subReport))
            expectedColumns = ReportDashboardCatalog.IsInvitationProcessLongSubReport(subReport)
                ? ReportDashboardInvitationTableExcel.ProcessCompanyLongHeaders.Length
                : ReportDashboardInvitationTableExcel.ProcessCompanyHeaders.Length;

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
        var key = CanonicalKey(category, subReport);
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

    private static string SheetName(ReportDashboardCategory category, string? subReport)
    {
        if (category != ReportDashboardCategory.WorkPermit)
            return "Report";
        if (string.Equals(subReport, ReportDashboardCatalog.WorkPermitCalismaIzniListesiKey, StringComparison.OrdinalIgnoreCase))
            return "çalışma izni listesi";
        if (string.Equals(subReport, ReportDashboardCatalog.WorkPermitSanawKey, StringComparison.OrdinalIgnoreCase))
            return "Sanaw";
        if (string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat3Key, StringComparison.OrdinalIgnoreCase))
            return "Goşmaça barjak ýerleri";
        if (string.Equals(subReport, ReportDashboardCatalog.WorkPermitMaglumat2Key, StringComparison.OrdinalIgnoreCase))
            return "A.Maglumat 2";
        if (string.Equals(subReport, "active-by-project", StringComparison.OrdinalIgnoreCase))
            return "A.Maglumat";
        return "Report";
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

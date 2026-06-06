using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

public enum ApplicationWordReportPackageReadinessLevel
{
    Ready = 0,
    Warning = 1
}

public enum ApplicationWordReportPackageEntrySource
{
    System = 0,
    User = 1
}

public sealed class ApplicationWordReportPackageReadinessSummary
{
    public int ReadyCount { get; init; }

    public int WarningCount { get; init; }

    public bool HasWarnings => WarningCount > 0;

    public static ApplicationWordReportPackageReadinessSummary Compute(
        IReadOnlyList<ApplicationWordReportPackageCatalogEntry> entries,
        IReadOnlySet<string>? selectedEntryKeys = null)
    {
        if (entries == null || entries.Count == 0)
        {
            return new ApplicationWordReportPackageReadinessSummary();
        }

        int ready = 0;
        int warning = 0;
        foreach (var entry in entries)
        {
            if (selectedEntryKeys != null
                && selectedEntryKeys.Count > 0
                && !selectedEntryKeys.Contains(entry.EntryKey))
            {
                continue;
            }

            if (entry.Readiness == ApplicationWordReportPackageReadinessLevel.Warning)
                warning++;
            else
                ready++;
        }

        return new ApplicationWordReportPackageReadinessSummary
        {
            ReadyCount = ready,
            WarningCount = warning
        };
    }
}

public static class ApplicationWordReportPackageReadinessEvaluator
{
    public static ApplicationWordReportPackageReadinessLevel EvaluateSystemReport() =>
        ApplicationWordReportPackageReadinessLevel.Ready;

    public static (ApplicationWordReportPackageReadinessLevel Level, string? MessageKey) EvaluateUserTemplate(
        IObjectSpace objectSpace,
        Application application,
        UserReportTemplate template)
    {
        if (template.TemplateFile == null || template.TemplateFile.Size <= 0)
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.NoTemplateFile");
        }

        var placeholders = template.Placeholders;
        if (placeholders == null || placeholders.Count == 0)
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.NotValidated");
        }

        if (placeholders.Any(p => !p.IsValid))
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.InvalidPlaceholders");
        }

        if (TemplateNeedsApplicationItems(template)
            && UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application).Count == 0)
        {
            return (ApplicationWordReportPackageReadinessLevel.Warning,
                "ApplicationReportPackage.Readiness.NoApplicationItems");
        }

        return (ApplicationWordReportPackageReadinessLevel.Ready, null);
    }

    private static bool TemplateNeedsApplicationItems(UserReportTemplate template)
    {
        if (template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel
            && template.ExcelMergeMode == ExcelMergeMode.ItemList)
        {
            return true;
        }

        if (template.RootBoType == UserReportBoType.ApplicationItem)
            return true;

        return template.Placeholders?
            .Any(p => PlaceholderKeyImpliesRows(p.PlaceholderKey)) == true;
    }

    private static bool PlaceholderKeyImpliesRows(string? placeholderKey)
    {
        if (string.IsNullOrWhiteSpace(placeholderKey))
            return false;

        return placeholderKey.Contains(".rows.", StringComparison.OrdinalIgnoreCase)
               || placeholderKey.Contains("{{#ds.rows", StringComparison.OrdinalIgnoreCase)
               || placeholderKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.WordReports;

/// <summary>
/// Lightweight merge dry-run for the report package catalog — surfaces likely empty fields before ZIP/preview.
/// </summary>
public static class ApplicationWordReportPackageDryRunEvaluator
{
    private const int MaxHints = 6;

    public static IReadOnlyList<ApplicationWordReportPackageReadinessHint> CollectUserTemplateHints(
        IObjectSpace objectSpace,
        Application application,
        UserReportTemplate template,
        IList<ApplicationItem>? selectedItems = null)
    {
        if (objectSpace == null || application == null || template == null)
            return [];

        var placeholders = template.Placeholders?
            .Where(p => p != null && p.IsValid)
            .ToList();

        if (placeholders == null || placeholders.Count == 0)
            return [];

        var hints = new List<ApplicationWordReportPackageReadinessHint>();
        var items = selectedItems
                    ?? UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application);
        var needsItems = TemplateNeedsApplicationItems(template);

        if (needsItems && items.Count == 0)
            return hints;

        foreach (var placeholder in placeholders)
        {
            if (IsLoopPlaceholder(placeholder) || IsImagePlaceholder(placeholder))
                continue;

            if (IsApplicationScalarPlaceholder(placeholder, template))
            {
                TryAddEmptyFieldHint(hints, application, placeholder.ResolvedPropertyPath);
                continue;
            }

            if (needsItems && IsRowPlaceholder(placeholder))
            {
                CollectRowFieldHints(hints, items, placeholder);
            }
        }

        if (TemplateUsesPhotoPlaceholder(placeholders) && items.Count > 0)
        {
            int missingPhotos = items.Count(item => item.Person_Photo == null || item.Person_Photo.Length == 0);
            if (missingPhotos > 0)
            {
                hints.Add(new ApplicationWordReportPackageReadinessHint
                {
                    MessageKey = "ApplicationReportPackage.Hint.MissingPhoto",
                    FormatArgs = [missingPhotos.ToString()]
                });
            }
        }

        return TrimHints(hints);
    }

    private static void CollectRowFieldHints(
        List<ApplicationWordReportPackageReadinessHint> hints,
        IList<ApplicationItem> items,
        UserReportPlaceholder placeholder)
    {
        var propertyPath = ResolveRowPropertyPath(placeholder);
        if (string.IsNullOrWhiteSpace(propertyPath)
            || IsPhotoProperty(propertyPath)
            || IsSyntheticRowNumberProperty(propertyPath))
            return;

        for (int i = 0; i < items.Count; i++)
        {
            if (hints.Count >= MaxHints)
                return;

            var value = UserReportMergeDataHelper.GetPropertyValue(items[i], propertyPath);
            if (!IsEmptyValue(value))
                continue;

            var lineLabel = items[i].ApplicationItemName ?? items[i].Person_FullName ?? (i + 1).ToString();
            hints.Add(new ApplicationWordReportPackageReadinessHint
            {
                MessageKey = "ApplicationReportPackage.Hint.EmptyItemField",
                FormatArgs = [lineLabel, propertyPath]
            });
        }
    }

    private static void TryAddEmptyFieldHint(
        List<ApplicationWordReportPackageReadinessHint> hints,
        Application application,
        string? propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath) || IsPhotoProperty(propertyPath))
            return;

        var value = UserReportMergeDataHelper.GetPropertyValue(application, propertyPath);
        if (!IsEmptyValue(value))
            return;

        hints.Add(new ApplicationWordReportPackageReadinessHint
        {
            MessageKey = "ApplicationReportPackage.Hint.EmptyApplicationField",
            FormatArgs = [propertyPath]
        });
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
            .Any(p => p.IsValid && PlaceholderKeyImpliesRows(p.PlaceholderKey)) == true;
    }

    private static bool PlaceholderKeyImpliesRows(string? placeholderKey)
    {
        if (string.IsNullOrWhiteSpace(placeholderKey))
            return false;

        return placeholderKey.Contains(".rows.", StringComparison.OrdinalIgnoreCase)
               || placeholderKey.Contains("{{#ds.rows", StringComparison.OrdinalIgnoreCase)
               || placeholderKey.StartsWith("rows.", StringComparison.OrdinalIgnoreCase)
               || placeholderKey.StartsWith(".", StringComparison.Ordinal);
    }

    private static bool IsApplicationScalarPlaceholder(UserReportPlaceholder placeholder, UserReportTemplate template) =>
        template.RootBoType == UserReportBoType.Application
        && !IsRowPlaceholder(placeholder)
        && !string.IsNullOrWhiteSpace(placeholder.ResolvedPropertyPath);

    private static bool IsRowPlaceholder(UserReportPlaceholder placeholder) =>
        placeholder.IsRowProperty
        || PlaceholderKeyImpliesRows(placeholder.PlaceholderKey);

    private static bool IsLoopPlaceholder(UserReportPlaceholder placeholder) =>
        placeholder.IsCollection
        || placeholder.PlaceholderKey.StartsWith("#", StringComparison.Ordinal);

    private static bool IsImagePlaceholder(UserReportPlaceholder placeholder) =>
        placeholder.PlaceholderKey.Contains("IMAGE:", StringComparison.OrdinalIgnoreCase);

    private static bool TemplateUsesPhotoPlaceholder(IEnumerable<UserReportPlaceholder> placeholders) =>
        placeholders.Any(p =>
            p.PlaceholderKey.Contains("Person_Photo", StringComparison.OrdinalIgnoreCase)
            || p.PlaceholderKey.Contains("IMAGE:", StringComparison.OrdinalIgnoreCase));

    private static string ResolveRowPropertyPath(UserReportPlaceholder placeholder)
    {
        if (!string.IsNullOrWhiteSpace(placeholder.ResolvedPropertyPath))
            return placeholder.ResolvedPropertyPath;

        var key = UserReportMergeDataHelper.StripDocxModelPrefix(placeholder.PlaceholderKey.TrimStart('#', '/'));
        if (key.StartsWith("rows.", StringComparison.OrdinalIgnoreCase) && key.Length > 5)
            return key[5..];

        if (key.StartsWith(".", StringComparison.Ordinal))
            return key[1..];

        return key;
    }

    private static bool IsPhotoProperty(string propertyPath) =>
        propertyPath.Contains("Photo", StringComparison.OrdinalIgnoreCase);

    /// <summary><c>RowNo</c> / <c>RowNumber</c> are assigned at merge time, not ApplicationItem BO properties.</summary>
    private static bool IsSyntheticRowNumberProperty(string propertyPath) =>
        string.Equals(propertyPath, "RowNo", StringComparison.OrdinalIgnoreCase)
        || string.Equals(propertyPath, "RowNumber", StringComparison.OrdinalIgnoreCase);

    private static bool IsEmptyValue(object? value) =>
        value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            byte[] bytes => bytes.Length == 0,
            bool b => !b,
            _ => false
        };

    private static IReadOnlyList<ApplicationWordReportPackageReadinessHint> TrimHints(List<ApplicationWordReportPackageReadinessHint> hints)
    {
        if (hints.Count <= MaxHints)
            return hints;

        var trimmed = hints.Take(MaxHints - 1).ToList();
        trimmed.Add(new ApplicationWordReportPackageReadinessHint
        {
            MessageKey = "ApplicationReportPackage.Hint.MoreIssues",
            FormatArgs = [(hints.Count - MaxHints + 1).ToString()]
        });
        return trimmed;
    }
}

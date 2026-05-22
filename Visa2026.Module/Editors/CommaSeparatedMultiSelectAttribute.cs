using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Editors;

/// <summary>
/// Configures the Blazor comma-separated multi-select property editor for a string property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CommaSeparatedMultiSelectAttribute : Attribute
{
    /// <summary>Lookup row type whose <c>NameTm</c> populates the catalog.</summary>
    public Type CatalogEntityType { get; init; } = null!;

    public bool AllowAddNew { get; init; } = true;

    public string NoneValue { get; init; } = CommaSeparatedSelectionHelper.NoneValue;

    public string PopupTitle { get; init; } = "Saýla";

    public string PopupButtonTitle { get; init; } = "Saýla";

    public string SearchPlaceholder { get; init; } = "Gözleg…";

    public string AddPlaceholder { get; init; } = "Täze at";

    public string AddButtonText { get; init; } = "Goş";

    public string ShowMoreText { get; init; } = "Hasabyny görkez";

    public string ShowLessText { get; init; } = "Gysga görkez";

    public string OkText { get; init; } = "OK";

    public string CancelText { get; init; } = "Ýatyr";

    public string SelectedCountFormat { get; init; } = "Saýlanan: {0}";
}

/// <summary>Registered <see cref="DevExpress.ExpressApp.Editors.EditorAliasAttribute"/> values.</summary>
public static class CommaSeparatedMultiSelectEditorAliases
{
    public const string BorderZone = "BorderZoneMultiSelect";

    public const string WorkPermittedLocation = "WorkPermittedLocationMultiSelect";
}

/// <summary>Default editor settings when <see cref="CommaSeparatedMultiSelectAttribute"/> is not on the property.</summary>
public static class CommaSeparatedMultiSelectDefaults
{
    public static CommaSeparatedMultiSelectAttribute ForEditorAlias(string? editorAlias) =>
        editorAlias switch
        {
            CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation => new CommaSeparatedMultiSelectAttribute
            {
                CatalogEntityType = typeof(WorkPermittedLocationName),
                NoneValue = string.Empty,
                PopupTitle = "Rugsat berlen ýerle",
                PopupButtonTitle = "Rugsat berlen ýerle",
                AddPlaceholder = "Täze rugsat berlen ýer",
            },
            _ => new CommaSeparatedMultiSelectAttribute
            {
                CatalogEntityType = typeof(BorderZoneName),
                NoneValue = CommaSeparatedSelectionHelper.NoneValue,
                PopupTitle = "Serhet sebitleri",
                PopupButtonTitle = "Serhet sebitlerini saýla",
                AddPlaceholder = "Täze serhet sebiti",
            },
        };
}

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



    /// <summary>Rename/delete catalog rows from the selection popup (requires Delete on catalog type).</summary>

    public bool AllowManageCatalog { get; init; } = true;



    public string NoneValue { get; init; } = CommaSeparatedSelectionHelper.NoneValue;



    public string PopupTitle { get; init; } = "Select";



    public string PopupButtonTitle { get; init; } = "Select";



    public string AddPlaceholder { get; init; } = "New name";



    public string AddButtonText { get; init; } = "Add";



    public string OkText { get; init; } = "OK";



    public string CancelText { get; init; } = "Cancel";



    public string SelectedCountFormat { get; init; } = "Selected: {0}";



    public string EditButtonText { get; init; } = "Edit";



    public string DeleteButtonText { get; init; } = "Delete";



    public string EditPopupTitle { get; init; } = "Rename";



    public string SaveEditText { get; init; } = "Save";



    public string RenameSuccessFormat { get; init; } = "Name updated.";



    public string DeleteConfirmTitle { get; init; } = "Delete?";



    public string DeleteConfirmFormat { get; init; } =

        "Remove \"{0}\" from the catalog? If it is used on any records, it will be removed there too.";



    public string DeleteSuccessFormat { get; init; } =

        "Removed from {0} record(s) and deleted from the catalog.";



    public string DeleteSuccessUnusedFormat { get; init; } = "Deleted from the catalog.";



    public string EmptyListMessage { get; init; } = "No catalog items.";

    public string ManageCatalogButtonText { get; init; } = "Manage catalog";

    public string DoneManageCatalogButtonText { get; init; } = "Done";

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

                PopupTitle = "Work permitted locations",

                PopupButtonTitle = "Select work permitted locations",

                AddPlaceholder = "New work permitted location",

            },

            _ => new CommaSeparatedMultiSelectAttribute

            {

                CatalogEntityType = typeof(BorderZoneName),

                NoneValue = CommaSeparatedSelectionHelper.NoneValue,

                PopupTitle = "Border zones",

                PopupButtonTitle = "Select border zones",

                AddPlaceholder = "New border zone",

            },

        };

}



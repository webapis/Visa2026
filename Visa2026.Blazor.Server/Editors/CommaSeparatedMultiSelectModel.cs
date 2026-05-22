using System;

using System.Collections.Generic;

using DevExpress.ExpressApp;

using DevExpress.ExpressApp.Blazor.Components.Models;

using Microsoft.AspNetCore.Components;

using Visa2026.Module.Services;



namespace Visa2026.Blazor.Server.Editors;



public class CommaSeparatedMultiSelectModel : ComponentModelBase

{

    public string DisplayText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public IEnumerable<string> CatalogItems

    {

        get => GetPropertyValue<IEnumerable<string>>() ?? Array.Empty<string>();

        set => SetPropertyValue(value);

    }



    public HashSet<string> SelectedItems

    {

        get => GetPropertyValue<HashSet<string>>() ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        set => SetPropertyValue(value);

    }



    public HashSet<string> DraftSelectedItems

    {

        get => GetPropertyValue<HashSet<string>>() ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        set => SetPropertyValue(value);

    }



    public EventCallback<HashSet<string>> SelectedItemsChanged

    {

        get => GetPropertyValue<EventCallback<HashSet<string>>>();

        set => SetPropertyValue(value);

    }



    public EventCallback<HashSet<string>> DraftSelectedItemsChanged

    {

        get => GetPropertyValue<EventCallback<HashSet<string>>>();

        set => SetPropertyValue(value);

    }



    public bool PopupVisible

    {

        get => GetPropertyValue<bool>();

        set => SetPropertyValue(value);

    }



    public EventCallback<bool> PopupVisibleChanged

    {

        get => GetPropertyValue<EventCallback<bool>>();

        set => SetPropertyValue(value);

    }



    public string NewItemName

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public EventCallback<string> NewItemNameChanged

    {

        get => GetPropertyValue<EventCallback<string>>();

        set => SetPropertyValue(value);

    }



    public EventCallback<string> AddItemRequested

    {

        get => GetPropertyValue<EventCallback<string>>();

        set => SetPropertyValue(value);

    }



    public bool AllowAddNew

    {

        get => GetPropertyValue<bool>();

        set => SetPropertyValue(value);

    }



    public bool AllowManageCatalog

    {

        get => GetPropertyValue<bool>();

        set => SetPropertyValue(value);

    }



    public string StatusMessage

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public bool StatusIsError

    {

        get => GetPropertyValue<bool>();

        set => SetPropertyValue(value);

    }



    public EventCallback<CatalogRenameRequest> RenameCatalogRequested

    {

        get => GetPropertyValue<EventCallback<CatalogRenameRequest>>();

        set => SetPropertyValue(value);

    }



    public EventCallback<string> DeleteCatalogRequested

    {

        get => GetPropertyValue<EventCallback<string>>();

        set => SetPropertyValue(value);

    }



    public string EditButtonText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string DeleteButtonText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string EditPopupTitle

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string SaveEditText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string DeleteConfirmTitle

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string DeleteConfirmFormat

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public bool ReadOnly

    {

        get => GetPropertyValue<bool>();

        set => SetPropertyValue(value);

    }



    public string PopupTitle

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string PopupButtonTitle

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string AddPlaceholder

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string AddButtonText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string OkText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string CancelText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string SelectedCountFormat

    {

        get => GetPropertyValue<string>() ?? "Selected: {0}";

        set => SetPropertyValue(value);

    }



    public string EmptyListMessage

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string ManageCatalogButtonText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public string DoneManageCatalogButtonText

    {

        get => GetPropertyValue<string>() ?? string.Empty;

        set => SetPropertyValue(value);

    }



    public IObjectSpace ObjectSpace

    {

        get => GetPropertyValue<IObjectSpace>();

        set => SetPropertyValue(value);

    }



    public override Type ComponentType => typeof(CommaSeparatedMultiSelectComponent);

}


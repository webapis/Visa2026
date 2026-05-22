using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

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

    public bool Expanded
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback<bool> ExpandedChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }

    public string FilterText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public EventCallback<string> FilterTextChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
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

    public bool ReadOnly
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public int CollapsedVisibleCount
    {
        get => GetPropertyValue<int>();
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

    public string SearchPlaceholder
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

    public string ShowMoreText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string ShowLessText
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
        get => GetPropertyValue<string>() ?? "Saýlanan: {0}";
        set => SetPropertyValue(value);
    }

    public IObjectSpace ObjectSpace
    {
        get => GetPropertyValue<IObjectSpace>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(CommaSeparatedMultiSelectComponent);
}

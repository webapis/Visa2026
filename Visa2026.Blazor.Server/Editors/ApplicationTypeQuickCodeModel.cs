using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

public class ApplicationTypeQuickCodeModel : ComponentModelBase
{
    public string Value
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public EventCallback<string> ValueChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public bool ReadOnly
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public IObjectSpace ObjectSpace
    {
        get => GetPropertyValue<IObjectSpace>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<ApplicationTypeCodePickerRow> PickerRows
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationTypeCodePickerRow>>() ?? Array.Empty<ApplicationTypeCodePickerRow>();
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

    public string PickerButtonTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string PopupTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string EmptyListMessage { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ColCode { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ColType { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ColGroup { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string CloseButtonText { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ApplyingMessage { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ColStatus { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string StatusReadyTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string StatusPendingTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string StatusNotReadyTitle { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ReadinessLegend { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }
    public string ReadinessBlockedMessage { get => GetPropertyValue<string>() ?? string.Empty; set => SetPropertyValue(value); }

    public override Type ComponentType => typeof(ApplicationTypeQuickCodeComponent);
}

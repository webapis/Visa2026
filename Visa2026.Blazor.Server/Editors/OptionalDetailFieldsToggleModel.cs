using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Visa2026.Blazor.Server.Editors;

public sealed class OptionalDetailFieldsToggleModel : ComponentModelBase
{
    public bool Value
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string ToolTip
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public EventCallback OnToggleAsync
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(OptionalDetailFieldsToggleComponent);
}

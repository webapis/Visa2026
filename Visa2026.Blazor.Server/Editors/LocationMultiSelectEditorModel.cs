using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Editors;

/// <summary>XAF ComponentModel host for <see cref="LocationMultiSelectField"/>.</summary>
public sealed class LocationMultiSelectEditorModel : ComponentModelBase
{
    public string? Value
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string?> ValueChanged
    {
        get => GetPropertyValue<EventCallback<string?>>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<CaseAddressAutocomplete.SavedEntry> Suggestions
    {
        get => GetPropertyValue<IReadOnlyList<CaseAddressAutocomplete.SavedEntry>>()
            ?? Array.Empty<CaseAddressAutocomplete.SavedEntry>();
        set => SetPropertyValue(value);
    }

    public bool Enabled
    {
        get => GetPropertyValue<bool?>() ?? true;
        set => SetPropertyValue(value);
    }

    public bool ReadOnly
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string? NoneValue
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string Placeholder
    {
        get => GetPropertyValue<string>() ?? "Search or type a new location";
        set => SetPropertyValue(value);
    }

    public string EmptyHint
    {
        get => GetPropertyValue<string>()
            ?? "Search saved locations, or type a new one and press Enter.";
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(LocationMultiSelectField);
}

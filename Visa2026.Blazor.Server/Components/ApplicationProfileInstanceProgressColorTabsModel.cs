using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Components;

/// <summary>
/// Mutable strip state for the progress-color ListView tabs.
/// DxGrid keeps the toolbar RenderFragment; the strip listens to <see cref="Changed"/> instead of stale closures.
/// </summary>
public sealed class ApplicationProfileInstanceProgressColorTabsModel
{
    public IReadOnlyList<ApplicationProfileInstanceProgressColorTab.TabInfo> Tabs { get; set; } =
        Array.Empty<ApplicationProfileInstanceProgressColorTab.TabInfo>();

    public string SelectedKey { get; set; } = ApplicationProfileInstanceProgressColorTab.AllKey;

    /// <summary>Tab key currently waiting for the grid to finish refreshing; null when idle.</summary>
    public string? LoadingKey { get; set; }

    public EventCallback<string> OnSelect { get; set; }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
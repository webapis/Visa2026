using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Blazor.Server.Components;

public sealed class WorkPermitItemColorTabsModel
{
    public IReadOnlyList<WorkPermitItemColorTab.TabInfo> Tabs { get; set; } =
        Array.Empty<WorkPermitItemColorTab.TabInfo>();

    public string SelectedKey { get; set; } = WorkPermitItemColorTab.AllKey;

    public string? LoadingKey { get; set; }

    public EventCallback<string> OnSelect { get; set; }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
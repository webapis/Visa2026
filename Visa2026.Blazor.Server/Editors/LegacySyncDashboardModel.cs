using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.LegacySyncDashboard;

namespace Visa2026.Blazor.Server.Editors;

public class LegacySyncDashboardModel : ComponentModelBase
{
    public override Type ComponentType => typeof(LegacySyncDashboardComponent);

    public int PollIntervalSeconds
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public LegacySyncDashboardSnapshot Snapshot
    {
        get => GetPropertyValue<LegacySyncDashboardSnapshot>() ?? new LegacySyncDashboardSnapshot();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}

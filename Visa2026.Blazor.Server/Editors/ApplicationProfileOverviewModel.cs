#nullable enable
using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationProfileOverview;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationProfileOverviewModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationProfileOverviewComponent);

    public ApplicationProfileOverviewSnapshot? Snapshot
    {
        get => GetPropertyValue<ApplicationProfileOverviewSnapshot?>();
        set => SetPropertyValue(value);
    }

    public bool IsLoading
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback InitialLoadRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public EventCallback ConfigureRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}

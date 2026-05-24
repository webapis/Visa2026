using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.BusinessObjects.StateNotifications;

namespace Visa2026.Blazor.Server.Editors;

public class BoStateNotificationInboxModel : ComponentModelBase
{
    public override Type ComponentType => typeof(BoStateNotificationInboxComponent);
    public IReadOnlyList<BoStateNotificationItem> Items
    {
        get => GetPropertyValue<IReadOnlyList<BoStateNotificationItem>>() ?? Array.Empty<BoStateNotificationItem>();
        set => SetPropertyValue(value);
    }

    public EventCallback<BoStateNotificationItem> OpenRecordRequested
    {
        get => GetPropertyValue<EventCallback<BoStateNotificationItem>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> ShowMessageRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback StateChanged
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}

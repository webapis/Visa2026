using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Components;

public sealed class PersonRosterColorTabsModel
{
    public IReadOnlyList<PersonRosterColorTab.TabInfo> Tabs { get; set; } =
        Array.Empty<PersonRosterColorTab.TabInfo>();

    public string SelectedKey { get; set; } = PersonRosterColorTab.All;

    public EventCallback<string> OnSelect { get; set; }

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}

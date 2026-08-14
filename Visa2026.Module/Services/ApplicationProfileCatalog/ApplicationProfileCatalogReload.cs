using System;
using System.Threading.Tasks;

#nullable enable

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public sealed class ApplicationProfileCatalogReload : IApplicationProfileCatalogReload
{
    public event Func<Task>? Reloading;

    public async Task RequestReloadAsync()
    {
        var handlers = Reloading;
        if (handlers == null)
            return;

        foreach (var d in handlers.GetInvocationList())
        {
            if (d is Func<Task> fn)
                await fn().ConfigureAwait(false);
        }
    }
}
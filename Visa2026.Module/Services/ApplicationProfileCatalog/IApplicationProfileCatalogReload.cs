using System;
using System.Threading.Tasks;

#nullable enable

namespace Visa2026.Module.Services.ApplicationProfileCatalog;

public interface IApplicationProfileCatalogReload
{
    event Func<Task>? Reloading;

    Task RequestReloadAsync();
}
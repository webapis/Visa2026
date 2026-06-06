using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationReportPackageModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationReportPackageComponent);

    public Guid ApplicationId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public WordReportPackageScope PackageScope
    {
        get => GetPropertyValue<WordReportPackageScope>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<Guid> ApplicationItemIds
    {
        get => GetPropertyValue<IReadOnlyList<Guid>>() ?? Array.Empty<Guid>();
        set => SetPropertyValue(value);
    }

    public string ApplicationNumber
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<ApplicationWordReportPackageCatalogEntry> CatalogEntries
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationWordReportPackageCatalogEntry>>()
               ?? Array.Empty<ApplicationWordReportPackageCatalogEntry>();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public string UiCultureName
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }
}

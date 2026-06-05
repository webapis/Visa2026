using System;
using System.Collections.Generic;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;
using Visa2026.Module.Services.ApplicationItemLinkedDocuments;

namespace Visa2026.Blazor.Server.Editors;

public sealed class ApplicationItemDocumentCopiesModel : ComponentModelBase
{
    public override Type ComponentType => typeof(ApplicationItemDocumentCopiesComponent);

    public IReadOnlyList<Guid> ApplicationItemIds
    {
        get => GetPropertyValue<IReadOnlyList<Guid>>() ?? Array.Empty<Guid>();
        set => SetPropertyValue(value);
    }

    public IReadOnlyList<ApplicationItemLinkedDocumentMergedGroup> MergedGroups
    {
        get => GetPropertyValue<IReadOnlyList<ApplicationItemLinkedDocumentMergedGroup>>()
               ?? Array.Empty<ApplicationItemLinkedDocumentMergedGroup>();
        set => SetPropertyValue(value);
    }

    public EventCallback RefreshRequested
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }
}

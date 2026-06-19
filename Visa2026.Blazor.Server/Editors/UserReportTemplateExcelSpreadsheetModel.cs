using System;
using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Visa2026.Blazor.Server.Editors;

public sealed class UserReportTemplateExcelSpreadsheetModel : ComponentModelBase
{
    public override Type ComponentType => typeof(UserReportTemplateExcelSpreadsheetPanel);

    public Guid TemplateId
    {
        get => GetPropertyValue<Guid>();
        set => SetPropertyValue(value);
    }

    public bool IsExcelTemplate
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string SpreadsheetUrl
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public EventCallback<string> ShowMessageRequested
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback<bool> DirtyStateChanged
    {
        get => GetPropertyValue<EventCallback<bool>>();
        set => SetPropertyValue(value);
    }

    public bool CanEdit
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public string SaveButtonText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string ReloadButtonText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string StatusSavedText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string StatusUnsavedText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string ReadOnlyText
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public string ReloadConfirmMessage
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }
}

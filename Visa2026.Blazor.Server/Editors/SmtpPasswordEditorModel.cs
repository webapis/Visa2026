using DevExpress.ExpressApp.Blazor.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Visa2026.Blazor.Server.Editors;

public sealed class SmtpPasswordEditorModel : ComponentModelBase
{
    public string Text
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    /// <summary>Decrypted password used only when the eye button reveals it. Not written back unless the officer edits it.</summary>
    public string SavedPassword
    {
        get => GetPropertyValue<string>() ?? string.Empty;
        set => SetPropertyValue(value);
    }

    public bool HasSavedPassword
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool Editing
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public bool Revealed
    {
        get => GetPropertyValue<bool>();
        set => SetPropertyValue(value);
    }

    public EventCallback<string> TextChanged
    {
        get => GetPropertyValue<EventCallback<string>>();
        set => SetPropertyValue(value);
    }

    public EventCallback ToggleReveal
    {
        get => GetPropertyValue<EventCallback>();
        set => SetPropertyValue(value);
    }

    public override Type ComponentType => typeof(SmtpPasswordEditorComponent);
}

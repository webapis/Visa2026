using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Editors;

/// <summary>
/// Masked mailbox password with a show/hide button. The browser password input shows bullets for the saved value.
/// </summary>
[PropertyEditor(typeof(string), SmtpPasswordEditorAliases.Editor, false)]
public sealed class SmtpPasswordPropertyEditor : BlazorPropertyEditorBase, IComplexViewItem
{
    private XafApplication? application;

    public SmtpPasswordPropertyEditor(Type objectType, IModelMemberViewItem model)
        : base(objectType, model)
    {
    }

    public override SmtpPasswordEditorModel ComponentModel =>
        (SmtpPasswordEditorModel)base.ComponentModel;

    void IComplexViewItem.Setup(IObjectSpace objectSpace, XafApplication application)
    {
        this.application = application;
    }

    protected override IComponentModel CreateComponentModel()
    {
        var model = new SmtpPasswordEditorModel();
        model.TextChanged = EventCallback.Factory.Create<string>(this, OnTextChanged);
        model.ToggleReveal = EventCallback.Factory.Create(this, OnToggleReveal);
        return model;
    }

    protected override void ReadValueCore()
    {
        base.ReadValueCore();
        if (ComponentModel == null || CurrentObject is not EmailSmtpSettings settings)
            return;

        var typed = settings.NewPassword;
        if (!string.IsNullOrEmpty(typed))
        {
            ComponentModel.Text = typed;
            ComponentModel.Editing = true;
        }

        ComponentModel.HasSavedPassword = !string.IsNullOrWhiteSpace(settings.ProtectedPassword);
        ComponentModel.SavedPassword = RevealStored(settings);
    }

    protected override object GetControlValueCore() =>
        ComponentModel is { Editing: true } ? ComponentModel.Text : string.Empty;

    private void OnTextChanged(string value)
    {
        if (ComponentModel != null)
        {
            ComponentModel.Editing = true;
            ComponentModel.Text = value ?? string.Empty;
        }

        PropertyValue = ComponentModel?.Text ?? string.Empty;
        WriteValue();
        OnControlValueChanged();
    }

    private void OnToggleReveal()
    {
        if (ComponentModel == null)
            return;
        ComponentModel.Revealed = !ComponentModel.Revealed;
    }

    private string RevealStored(EmailSmtpSettings settings)
    {
        var stored = settings.ProtectedPassword;
        if (string.IsNullOrWhiteSpace(stored))
            return string.Empty;

        var protector = application?.ServiceProvider?.GetService<IOfficerMailSecretProtector>();
        var plain = protector?.Unprotect(stored) ?? stored;
        if (string.IsNullOrWhiteSpace(plain)
            || plain.StartsWith(IOfficerMailSecretProtector.Prefix, StringComparison.Ordinal)
            || plain.Length > 80)
            return string.Empty;

        return plain;
    }
}

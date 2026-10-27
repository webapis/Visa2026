using System.ComponentModel;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Blazor.Components.Models;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Blazor.Server.Controllers;

/// <summary>
/// The Mail server password box does not round-trip through Save on its own.
/// Copy its text into <see cref="EmailSmtpSettings.ProtectedPassword"/> before commit.
/// </summary>
public sealed class EmailSmtpSettingsPasswordController : ObjectViewController<DetailView, EmailSmtpSettings>
{
    private StringPropertyEditor? passwordEditor;
    private DxTextBoxModel? passwordBox;
    private string? capturedPassword;
    private SimpleAction? saveAction;
    private SimpleAction? saveAndCloseAction;

    protected override void OnActivated()
    {
        base.OnActivated();
        ObjectSpace.Committing += ObjectSpace_Committing;
        ObjectSpace.ObjectSaving += ObjectSpace_ObjectSaving;

        var modifications = Frame.GetController<ModificationsController>();
        saveAction = modifications?.SaveAction;
        if (saveAction != null)
            saveAction.Executing += SaveAction_Executing;
        saveAndCloseAction = modifications?.SaveAndCloseAction;
        if (saveAndCloseAction != null)
            saveAndCloseAction.Executing += SaveAction_Executing;

        View.CustomizeViewItemControl<StringPropertyEditor>(
            this,
            ConfigurePasswordEditor,
            nameof(EmailSmtpSettings.NewPassword));
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.Committing -= ObjectSpace_Committing;
        ObjectSpace.ObjectSaving -= ObjectSpace_ObjectSaving;
        if (saveAction != null)
            saveAction.Executing -= SaveAction_Executing;
        saveAction = null;
        if (saveAndCloseAction != null)
            saveAndCloseAction.Executing -= SaveAction_Executing;
        saveAndCloseAction = null;
        passwordEditor = null;
        passwordBox = null;
        base.OnDeactivated();
    }

    private void ConfigurePasswordEditor(StringPropertyEditor editor)
    {
        passwordEditor = editor;
        editor.IsPassword = true;
        if (editor.ComponentModel is not DxTextBoxModel box)
            return;

        passwordBox = box;
        box.Password = true;
        // Push each keystroke to the server so Save and Close still sees the value.
        box.BindValueMode = BindValueMode.OnInput;
    }

    private void SaveAction_Executing(object? sender, CancelEventArgs e)
    {
        Capture();
        if (string.IsNullOrWhiteSpace(capturedPassword))
            return;
        if (View.CurrentObject is EmailSmtpSettings)
            ObjectSpace.SetModified(View.CurrentObject);
    }

    private void ObjectSpace_Committing(object? sender, CancelEventArgs e) =>
        StoreCapturedPassword();

    private void ObjectSpace_ObjectSaving(object? sender, ObjectManipulatingEventArgs e)
    {
        if (e.Object is EmailSmtpSettings settings)
            StoreCapturedPassword(settings);
    }

    private void Capture()
    {
        var text = passwordBox?.Text;
        if (string.IsNullOrWhiteSpace(text) && passwordEditor?.ControlValue is string controlText)
            text = controlText;
        if (string.IsNullOrWhiteSpace(text))
            return;
        if (text.StartsWith(IOfficerMailSecretProtector.Prefix, StringComparison.Ordinal))
            return;
        capturedPassword = text;
    }

    private void StoreCapturedPassword(EmailSmtpSettings? settings = null)
    {
        settings ??= View?.CurrentObject as EmailSmtpSettings;
        if (settings == null)
            return;

        Capture();
        var typed = capturedPassword;
        if (string.IsNullOrWhiteSpace(typed)
            && !string.IsNullOrWhiteSpace(settings.ProtectedPassword))
        {
            typed = settings.ProtectedPassword;
        }

        if (string.IsNullOrWhiteSpace(typed))
            return;

        var protector = Application?.ServiceProvider?.GetService<IOfficerMailSecretProtector>();
        if (protector?.IsProtected(typed) == true)
            return;

        settings.ProtectedPassword = protector == null ? typed : protector.Protect(typed);
        capturedPassword = null;
    }
}

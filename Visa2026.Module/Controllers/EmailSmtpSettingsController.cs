using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects.Email;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.EmailDocumentCopies;

namespace Visa2026.Module.Controllers;

/// <summary>
/// One Mail server row: no New/Delete, and Test connection.
/// The Blazor host copies the password box into the encrypted column on save.
/// </summary>
public sealed class EmailSmtpSettingsController : ObjectViewController<ObjectView, EmailSmtpSettings>
{
    private readonly SimpleAction testAction;
    private PropertyEditor? passwordEditor;

    public EmailSmtpSettingsController()
    {
        testAction = new SimpleAction(this, "TestEmailSmtpSettings", PredefinedCategory.View)
        {
            ImageName = "Actions_Check",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        testAction.Execute += TestAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        testAction.Caption = VisaUiMessages.Get("EmailDocumentCopies.Smtp.Test");
        View.AllowNew.SetItemValue(nameof(EmailSmtpSettingsController), false);
        View.AllowDelete.SetItemValue(nameof(EmailSmtpSettingsController), false);
        if (View is ListView)
        {
            var settings = EmailSmtpSettings.GetOrCreateInstance(ObjectSpace);
            if (ObjectSpace.IsNewObject(settings) || ObjectSpace.IsModified)
                ObjectSpace.CommitChanges();
            View.ObjectSpace.Refresh();
        }

    }

    protected override void OnViewControlsCreated()
    {
        base.OnViewControlsCreated();
        DetachPasswordEditor();
        if (View is not DetailView detail)
            return;
        passwordEditor = detail.FindItem(nameof(EmailSmtpSettings.NewPassword)) as PropertyEditor;
        if (passwordEditor != null)
        {
            passwordEditor.IsPassword = true;
            passwordEditor.ControlValueChanged += PasswordEditor_ControlValueChanged;
        }
    }

    protected override void OnDeactivated()
    {
        DetachPasswordEditor();
        View.AllowNew.RemoveItem(nameof(EmailSmtpSettingsController));
        View.AllowDelete.RemoveItem(nameof(EmailSmtpSettingsController));
        base.OnDeactivated();
    }

    private void PasswordEditor_ControlValueChanged(object? sender, EventArgs e)
    {
        if (View.CurrentObject is EmailSmtpSettings)
            ObjectSpace.SetModified(View.CurrentObject);
    }

    private void DetachPasswordEditor()
    {
        if (passwordEditor != null)
            passwordEditor.ControlValueChanged -= PasswordEditor_ControlValueChanged;
        passwordEditor = null;
    }

    private void TestAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (ObjectSpace.IsModified)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("EmailDocumentCopies.Smtp.Test.SaveFirst"),
                InformationType.Warning);
            return;
        }

        var mailSender = Application.ServiceProvider?.GetService<IOfficerMailSender>();
        if (mailSender == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("EmailDocumentCopies.Error.Unavailable"),
                InformationType.Error);
            return;
        }

        try
        {
            // Run off the Blazor circuit. Waiting here on the circuit deadlocks and leaves Loading up.
            var error = Task.Run(() => mailSender.TestConnectionAsync()).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(error))
            {
                Application.ShowViewStrategy.ShowMessage(
                    VisaUiMessages.Get("EmailDocumentCopies.Smtp.Test.Ok"),
                    InformationType.Success);
            }
            else
            {
                Application.ShowViewStrategy.ShowMessage(error, InformationType.Error);
            }
        }
        catch (OfficerMailException ex)
        {
            Application.ShowViewStrategy.ShowMessage(ex.Message, InformationType.Error);
        }
    }
}

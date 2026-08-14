using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the ministry letter copies catalog for the parent application from an <see cref="ApplicationProfileInstanceProgress"/> ListView.
/// </summary>
public sealed class ApplicationProfileInstanceProgressMinistryLettersController : ViewController<ListView>
{
    private SimpleAction ministryLettersAction;

    public ApplicationProfileInstanceProgressMinistryLettersController()
    {
        TargetObjectType = typeof(ApplicationProfileInstanceProgress);

        ministryLettersAction = new SimpleAction(this, "ViewApplicationProfileInstanceProgressMinistryLetters", "View");
        ministryLettersAction.ImageName = "BO_FileAttachment";
        ministryLettersAction.SelectionDependencyType = SelectionDependencyType.Independent;
        ministryLettersAction.Execute += MinistryLettersAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        ministryLettersAction.Caption = VisaUiMessages.Get("ApplicationProfileInstanceProgress.MinistryLetters.Title");
        UpdateActionState();
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var applicationId = ApplicationProfileInstanceProgressParentContext.GetApplicationProfileInstanceId(Frame, ObjectSpace, View);
        ministryLettersAction.Enabled["Application"] = applicationId != Guid.Empty;
    }

    private void MinistryLettersAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var applicationId = ApplicationProfileInstanceProgressParentContext.GetApplicationProfileInstanceId(Frame, ObjectSpace, View);
        if (applicationId == Guid.Empty)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationProfileInstanceProgress.MinistryLetters.NoApplication"),
                InformationType.Warning);
            return;
        }

        var slotService = Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("ApplicationItemDocumentCopies.Preview.Error"),
                InformationType.Error);
            return;
        }

        slotService.OpenProgressLettersAsync(new ProgressLettersSlotRequest
        {
            ApplicationProfileInstanceId = applicationId,
        }, VisaPreviewSlotViewHelper.ResolveOwnerViewId(View)).GetAwaiter().GetResult();
    }
}

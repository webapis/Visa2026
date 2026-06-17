using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.Extensions.DependencyInjection;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens person-scoped document copies from <see cref="Person"/> DetailView.
/// </summary>
public sealed class PersonDocumentCopiesController : ObjectViewController<DetailView, Person>
{
    private SimpleAction viewDocumentCopiesAction = null!;

    public PersonDocumentCopiesController()
    {
        viewDocumentCopiesAction = new SimpleAction(this, "ViewPersonDocumentCopies", "View");
        viewDocumentCopiesAction.ImageName = "BO_FileAttachment";
        viewDocumentCopiesAction.Execute += ViewDocumentCopiesAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        viewDocumentCopiesAction.Caption = VisaUiMessages.Get("PersonDocumentCopies.Title");
        View.CurrentObjectChanged += View_CurrentObjectChanged;
        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        viewDocumentCopiesAction.Enabled["Person"] = ViewCurrentObject != null;
    }

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var person = ViewCurrentObject;
        if (person == null)
            return;

        var personId = View.ObjectSpace.GetKeyValue(person) is Guid guid
            ? guid
            : Guid.Parse(Convert.ToString(View.ObjectSpace.GetKeyValue(person), System.Globalization.CultureInfo.InvariantCulture)!);

        if (personId == Guid.Empty)
            return;

        var slotService = Application.ServiceProvider.GetService<IVisaPreviewSlotService>();
        if (slotService == null)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonDocumentCopies.Preview.Error"),
                InformationType.Error);
            return;
        }

        slotService.OpenPersonDocumentCopiesAsync(new PersonDocumentCopiesSlotRequest
        {
            PersonIds = new[] { personId },
        }, VisaPreviewSlotViewHelper.ResolveOwnerViewId(View)).GetAwaiter().GetResult();
    }
}

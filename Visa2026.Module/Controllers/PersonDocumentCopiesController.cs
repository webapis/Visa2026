using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens person-scoped document copies from <see cref="Person"/> DetailView.
/// ListViews use the per-row Copies column instead (no toolbar action).
/// </summary>
public sealed class PersonDocumentCopiesController : ViewController<DetailView>
{
    private SimpleAction viewDocumentCopiesAction = null!;

    public PersonDocumentCopiesController()
    {
        TargetObjectType = typeof(Person);

        viewDocumentCopiesAction = new SimpleAction(this, "ViewPersonDocumentCopies", "View");
        viewDocumentCopiesAction.ImageName = "DocumentCopies";
        viewDocumentCopiesAction.SelectionDependencyType = SelectionDependencyType.Independent;
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

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState() =>
        viewDocumentCopiesAction.Enabled["Person"] = View.CurrentObject is Person;

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not Person person)
            return;

        PersonDocumentCopiesOpenHelper.TryOpenForPerson(Application, View, person);
    }
}

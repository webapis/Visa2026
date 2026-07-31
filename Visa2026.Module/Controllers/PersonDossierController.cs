using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the read-only person dossier from <see cref="Person"/> DetailView.
/// ListViews use the per-row Dossier column instead (no toolbar action).
/// </summary>
public sealed class PersonDossierController : ViewController<DetailView>
{
    private readonly SimpleAction openDossierAction;

    public PersonDossierController()
    {
        TargetObjectType = typeof(Person);

        openDossierAction = new SimpleAction(this, "OpenPersonDossier", "View");
        openDossierAction.ImageName = "BO_Person";
        openDossierAction.SelectionDependencyType = SelectionDependencyType.Independent;
        openDossierAction.Execute += OpenDossierAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        openDossierAction.Caption = VisaUiMessages.Get("PersonDossier.Action.Open");
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
        openDossierAction.Enabled["Person"] =
            View.CurrentObject is Person person && !ObjectSpace.IsNewObject(person);

    private void OpenDossierAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not Person person)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonDossier.List.SelectOne"),
                InformationType.Warning);
            return;
        }

        var dossierView = PersonDossierOpenHelper.CreateDossierView(Application, ObjectSpace, person);
        if (dossierView == null)
            return;

        e.ShowViewParameters.CreatedView = dossierView;
        e.ShowViewParameters.TargetWindow = TargetWindow.Current;
    }
}

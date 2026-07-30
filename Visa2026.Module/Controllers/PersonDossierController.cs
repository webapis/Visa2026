using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens the read-only person dossier from <see cref="Person"/> DetailView or typed ListViews.
/// </summary>
public sealed class PersonDossierController : ViewController
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

        if (View is DetailView)
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        else if (View is ListView)
            View.SelectionChanged += View_SelectionChanged;

        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        if (View is DetailView)
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
        else if (View is ListView)
            View.SelectionChanged -= View_SelectionChanged;

        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void View_SelectionChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        if (View is DetailView detailView)
        {
            // A dossier for an unsaved person would be empty.
            openDossierAction.Enabled["Person"] =
                detailView.CurrentObject is Person person && !ObjectSpace.IsNewObject(person);
            return;
        }

        if (View is ListView)
            openDossierAction.Enabled["Selection"] = GetSelectedPeople().Count == 1;
    }

    private void OpenDossierAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        Person? person = View switch
        {
            DetailView detailView => detailView.CurrentObject as Person,
            ListView => GetSelectedPeople().FirstOrDefault(),
            _ => null,
        };

        if (person == null)
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

    private List<Person> GetSelectedPeople()
    {
        if (View is not ListView listView)
            return new List<Person>();

        var selected = listView.SelectedObjects?
            .OfType<Person>()
            .Where(person => person != null)
            .ToList();

        if (selected is { Count: > 0 })
            return selected;

        return listView.CurrentObject is Person current
            ? new List<Person> { current }
            : new List<Person>();
    }
}

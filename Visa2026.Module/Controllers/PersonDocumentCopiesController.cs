using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.PersonLinkedDocuments;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens person-scoped document copies from <see cref="Person"/> DetailView or typed ListViews.
/// </summary>
public sealed class PersonDocumentCopiesController : ViewController
{
    private SimpleAction viewDocumentCopiesAction = null!;

    public PersonDocumentCopiesController()
    {
        TargetObjectType = typeof(Person);

        viewDocumentCopiesAction = new SimpleAction(this, "ViewPersonDocumentCopies", "View");
        viewDocumentCopiesAction.ImageName = "BO_FileAttachment";
        viewDocumentCopiesAction.SelectionDependencyType = SelectionDependencyType.Independent;
        viewDocumentCopiesAction.Execute += ViewDocumentCopiesAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        viewDocumentCopiesAction.Caption = VisaUiMessages.Get("PersonDocumentCopies.Title");

        if (View is DetailView)
        {
            View.CurrentObjectChanged += View_CurrentObjectChanged;
        }
        else if (View is ListView)
        {
            View.SelectionChanged += View_SelectionChanged;
        }

        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        if (View is DetailView)
        {
            View.CurrentObjectChanged -= View_CurrentObjectChanged;
        }
        else if (View is ListView)
        {
            View.SelectionChanged -= View_SelectionChanged;
        }

        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void View_SelectionChanged(object sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        if (View is DetailView detailView)
        {
            viewDocumentCopiesAction.Enabled["Person"] = detailView.CurrentObject is Person;
            return;
        }

        if (View is ListView)
        {
            viewDocumentCopiesAction.Enabled["Selection"] = GetSelectedPeople().Count == 1;
        }
    }

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View is DetailView detailView)
        {
            if (detailView.CurrentObject is not Person person)
                return;

            PersonDocumentCopiesOpenHelper.TryOpenForPerson(Application, View, person);
            return;
        }

        if (View is not ListView)
            return;

        var selected = GetSelectedPeople();
        if (selected.Count != 1)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonDocumentCopies.List.SelectOne"),
                InformationType.Warning);
            return;
        }

        PersonDocumentCopiesOpenHelper.TryOpenForPerson(Application, View, selected[0]);
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

        if (listView.CurrentObject is Person current)
            return new List<Person> { current };

        return new List<Person>();
    }
}

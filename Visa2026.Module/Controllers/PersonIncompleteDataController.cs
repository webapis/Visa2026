using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Controllers;

/// <summary>
/// DetailView actions: Mark incomplete (popup checkboxes + notes) and Mark complete (clears flag/notes).
/// Soft flag only — does not block applications.
/// </summary>
public class PersonIncompleteDataController : ObjectViewController<DetailView, Person>
{
    private readonly PopupWindowShowAction _markIncompleteAction;
    private readonly SimpleAction _markCompleteAction;

    public PersonIncompleteDataController()
    {
        _markIncompleteAction = new PopupWindowShowAction(this, "MarkPersonIncomplete", PredefinedCategory.Edit)
        {
            Caption = VisaUiMessages.Get("PersonIncomplete.Action.MarkIncomplete"),
            ImageName = "Action_Deny",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject
        };
        _markIncompleteAction.CustomizePopupWindowParams += MarkIncomplete_CustomizePopupWindowParams;
        _markIncompleteAction.Execute += MarkIncomplete_Execute;

        _markCompleteAction = new SimpleAction(this, "MarkPersonComplete", PredefinedCategory.Edit)
        {
            Caption = VisaUiMessages.Get("PersonIncomplete.Action.MarkComplete"),
            ImageName = "Action_Grant",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
            ConfirmationMessage = VisaUiMessages.Get("PersonIncomplete.Action.MarkComplete.Confirmation")
        };
        _markCompleteAction.Execute += MarkComplete_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        _markCompleteAction.Caption = VisaUiMessages.Get("PersonIncomplete.Action.MarkComplete");
        _markCompleteAction.ConfirmationMessage =
            VisaUiMessages.Get("PersonIncomplete.Action.MarkComplete.Confirmation");
        UpdateActionState();
        ObjectSpace.ObjectChanged += ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        ObjectSpace.ObjectChanged -= ObjectSpace_ObjectChanged;
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object sender, EventArgs e) => UpdateActionState();

    private void ObjectSpace_ObjectChanged(object sender, ObjectChangedEventArgs e)
    {
        if (e.Object == View.CurrentObject && e.PropertyName == nameof(Person.IsDataIncomplete))
            UpdateActionState();
    }

    private void UpdateActionState()
    {
        var person = View.CurrentObject as Person;
        var incomplete = person?.IsDataIncomplete == true;
        _markIncompleteAction.Active["PersonIncomplete"] = person != null;
        _markIncompleteAction.Caption = incomplete
            ? VisaUiMessages.Get("PersonIncomplete.Action.UpdateIncomplete")
            : VisaUiMessages.Get("PersonIncomplete.Action.MarkIncomplete");
        _markCompleteAction.Active["PersonIncomplete"] = person != null && incomplete;
    }

    private void MarkIncomplete_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
    {
        var person = View.CurrentObject as Person;
        var os = Application.CreateObjectSpace(typeof(PersonIncompleteMarkOptions));
        var opts = os.CreateObject<PersonIncompleteMarkOptions>();
        if (person != null)
            opts.LoadFrom(person);

        var detailView = Application.CreateDetailView(os, opts, true);
        detailView.ViewEditMode = ViewEditMode.Edit;
        e.View = detailView;
        e.DialogController.SaveOnAccept = false;
        e.DialogController.AcceptAction.Caption = VisaUiMessages.Get("PersonIncomplete.Dialog.Apply");
    }

    private void MarkIncomplete_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
    {
        if (e.PopupWindowView is not DetailView { CurrentObject: PersonIncompleteMarkOptions opts })
            return;
        if (View.CurrentObject is not Person person)
            return;

        if (!opts.HasAtLeastOneMissingArea)
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonIncomplete.Message.SelectAtLeastOneArea"),
                InformationType.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.Notes))
        {
            Application.ShowViewStrategy.ShowMessage(
                VisaUiMessages.Get("PersonIncomplete.Message.NotesRequired"),
                InformationType.Warning);
            return;
        }

        opts.ApplyTo(person, SecuritySystem.CurrentUserName ?? string.Empty);
        ObjectSpace.CommitChanges();
        UpdateActionState();
        View.Refresh(true);
    }

    private void MarkComplete_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not Person person)
            return;

        person.IsDataIncomplete = false;
        person.IncompleteMissingPersonalData = false;
        person.IncompleteMissingPassport = false;
        person.IncompleteMissingCv = false;
        person.IncompleteMissingPhoto = false;
        person.IncompleteMissingEducation = false;
        person.IncompleteMissingMedical = false;
        person.IncompleteMissingAddress = false;
        person.IncompleteMissingFamilyDocs = false;
        person.IncompleteMissingOther = false;
        person.IncompleteNotes = null;
        person.IncompleteMarkedOn = null;
        person.IncompleteMarkedBy = null;
        ObjectSpace.CommitChanges();
        UpdateActionState();
        View.Refresh(true);
    }
}

using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Starts a new Application from a Person DetailView (slice 11).
/// </summary>
public sealed class PersonStartApplicationController : ObjectViewController<DetailView, Person>
{
    private readonly SimpleAction _startApplicationAction;

    public PersonStartApplicationController()
    {
        _startApplicationAction = new SimpleAction(this, "PersonStartApplication", "View")
        {
            Caption = "Start application…",
            ImageName = "Action_New",
            ToolTip = "Pick an Application Profile and link people to a new Application.",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
        };
        _startApplicationAction.Execute += StartApplication_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        UpdateActionState();
        View.CurrentObjectChanged += View_CurrentObjectChanged;
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= View_CurrentObjectChanged;
        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var person = View.CurrentObject as Person;
        _startApplicationAction.Enabled["Person"] = person != null && !ObjectSpace.IsNewObject(person);
    }

    private void StartApplication_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not Person person || ObjectSpace.IsNewObject(person))
            return;

        var pickerView = ApplicationProfilePickerOpenHelper.CreatePersonStartPickerView(
            Application,
            person.ID,
            stayOnSourceAfterCreate: false,
            Frame);
        if (pickerView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(pickerView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));
    }
}

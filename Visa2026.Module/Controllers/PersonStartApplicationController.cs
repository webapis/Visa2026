using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationProfilePicker;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Starts a new ApplicationProfileInstance from a Person DetailView.
/// Retired: officers create instances only from Application Profile Instances lists.
/// </summary>
public sealed class PersonStartApplicationController : ObjectViewController<DetailView, Person>
{
    private readonly SimpleAction _startApplicationAction;

    public PersonStartApplicationController()
    {
        _startApplicationAction = new SimpleAction(this, "PersonStartApplication", "View")
        {
            Caption = "Start process…",
            ImageName = "Action_New",
            ToolTip = "Pick an Application Profile and link people to a new profile instance.",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
        };
        // Officers create Application Profile Instances only from Application Profile Instances lists.
        _startApplicationAction.Active["PersonDetail"] = false;
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

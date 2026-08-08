using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects.PersonDossier;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.PersonDossier;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Starts a new Application from Person Dossier (slice 11).
/// </summary>
public sealed class PersonDossierStartApplicationController : ViewController<DetailView>
{
    private readonly SimpleAction _startApplicationAction;

    public PersonDossierStartApplicationController()
    {
        TargetViewId = PersonDossierViewIds.DetailView;

        _startApplicationAction = new SimpleAction(this, "PersonDossierStartApplication", "View")
        {
            Caption = "Start application…",
            ImageName = "Action_New",
            ToolTip = "Pick an Application Profile and link people to a new Application.",
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
        _startApplicationAction.Enabled["Person"] = ResolvePersonId() != Guid.Empty;
    }

    private void StartApplication_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        var personId = ResolvePersonId();
        if (personId == Guid.Empty)
            return;

        var pickerView = ApplicationProfilePickerOpenHelper.CreatePersonStartPickerView(
            Application,
            personId,
            stayOnSourceAfterCreate: true,
            Frame);
        if (pickerView == null)
            return;

        Application.ShowViewStrategy.ShowView(
            new ShowViewParameters(pickerView) { TargetWindow = TargetWindow.Current },
            new ShowViewSource(Frame, null));
    }

    private Guid ResolvePersonId()
    {
        if (View.CurrentObject is PersonDossierHost host && host.PersonId != Guid.Empty)
            return host.PersonId;

        return PersonDossierPendingOpenGate.Get(Application);
    }
}

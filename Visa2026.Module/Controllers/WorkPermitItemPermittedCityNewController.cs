using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Controllers;

// Allow adding the first WorkPermittedCityLinks row even when the parent WorkPermitItem
// currently violates the "at least one permitted city" save rule (XAF may auto-save before New).
public class WorkPermitItemPermittedCityNewController : ViewController<DetailView> {
    public WorkPermitItemPermittedCityNewController() {
        TargetObjectType = typeof(WorkPermitItem);
        TargetViewId = "WorkPermitItem_DetailView";
    }

    protected override void OnViewControlsCreated() {
        base.OnViewControlsCreated();

        var cityLinksEditor = View
            .GetItems<ListPropertyEditor>()
            .FirstOrDefault(i => i.MemberInfo?.Name == nameof(WorkPermitItem.WorkPermittedCityLinks));

        var nestedFrame = cityLinksEditor?.Frame;
        var newObjectController = nestedFrame?.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null) {
            newObjectController.NewObjectAction.Executing += NewObjectAction_Executing;
        }
    }

    private void NewObjectAction_Executing(object sender, CancelEventArgs e) {
        if (View?.CurrentObject is not WorkPermitItem parent) {
            return;
        }

        // Cancel default behavior (which may attempt to save parent first).
        e.Cancel = true;

        var link = ObjectSpace.CreateObject<WorkPermitItemPermittedCity>();
        parent.WorkPermittedCityLinks.Add(link);

        // Refresh so the new row appears immediately.
        View.Refresh();
    }

    protected override void OnDeactivated() {
        var cityLinksEditor = View?
            .GetItems<ListPropertyEditor>()
            .FirstOrDefault(i => i.MemberInfo?.Name == nameof(WorkPermitItem.WorkPermittedCityLinks));

        var nestedFrame = cityLinksEditor?.Frame;
        var newObjectController = nestedFrame?.GetController<NewObjectViewController>();
        if (newObjectController?.NewObjectAction != null) {
            newObjectController.NewObjectAction.Executing -= NewObjectAction_Executing;
        }

        base.OnDeactivated();
    }
}


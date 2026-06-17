using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens header-scoped document copies for work permit, invitation, rejection, and border zone parents and items.
/// </summary>
public sealed class HeaderDocumentCopiesController : ViewController
{
    private static readonly HashSet<Type> SupportedTypes = new()
    {
        typeof(WorkPermit),
        typeof(WorkPermitItem),
        typeof(Invitation),
        typeof(InvitationItem),
        typeof(Rejection),
        typeof(RejectionItem),
        typeof(BorderZone),
        typeof(BorderZoneItem),
    };

    private SimpleAction viewDocumentCopiesAction = null!;

    public HeaderDocumentCopiesController()
    {
        viewDocumentCopiesAction = new SimpleAction(this, "ViewHeaderDocumentCopies", "View");
        viewDocumentCopiesAction.ImageName = "BO_FileAttachment";
        viewDocumentCopiesAction.SelectionDependencyType = SelectionDependencyType.Independent;
        viewDocumentCopiesAction.Execute += ViewDocumentCopiesAction_Execute;
    }

    protected override void OnActivated()
    {
        base.OnActivated();

        var objectType = View?.ObjectTypeInfo?.Type;
        Active["SupportedType"] = objectType != null && SupportedTypes.Contains(objectType);

        if (!Active["SupportedType"])
            return;

        if (HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(objectType, out var family))
            viewDocumentCopiesAction.Caption = HeaderDocumentCopiesLocalization.Title(family);

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

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void View_SelectionChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        if (View is DetailView detailView)
        {
            viewDocumentCopiesAction.Enabled["Object"] = CanOpen(detailView.CurrentObject);
            return;
        }

        if (View is ListView)
            viewDocumentCopiesAction.Enabled["Selection"] = GetSelectedObjects().Count == 1;
    }

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View is DetailView detailView)
        {
            HeaderDocumentCopiesOpenHelper.TryOpenFromViewObject(Application, View, detailView.CurrentObject);
            return;
        }

        if (View is not ListView)
            return;

        var selected = GetSelectedObjects();
        if (selected.Count != 1)
        {
            if (HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(View.ObjectTypeInfo?.Type, out var family))
            {
                Application.ShowViewStrategy.ShowMessage(
                    Visa2026.Module.Localization.VisaUiMessages.Get(
                        HeaderDocumentCopiesLocalization.ListSelectOneKey(family)),
                    InformationType.Warning);
            }

            return;
        }

        HeaderDocumentCopiesOpenHelper.TryOpenFromViewObject(Application, View, selected[0]);
    }

    private List<object> GetSelectedObjects()
    {
        if (View is not ListView listView)
            return new List<object>();

        return listView.SelectedObjects.Cast<object>().Where(o => o != null).ToList();
    }

    private static bool CanOpen(object? target) =>
        target switch
        {
            WorkPermit => true,
            WorkPermitItem item => item.WorkPermit != null,
            Invitation => true,
            InvitationItem item => item.Invitation != null,
            Rejection => true,
            RejectionItem item => item.Rejection != null,
            BorderZone => true,
            BorderZoneItem item => item.BorderZone != null,
            _ => false,
        };
}

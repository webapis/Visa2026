using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Opens header-scoped document copies from DetailView.
/// ListViews use the per-row Copies column instead (no toolbar action).
/// </summary>
public sealed class HeaderDocumentCopiesController : ViewController<DetailView>
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
        viewDocumentCopiesAction.ImageName = "DocumentCopies";
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

        if (HeaderDocumentCopiesOpenHelper.TryGetFamilyForType(objectType!, out var family))
            viewDocumentCopiesAction.Caption = HeaderDocumentCopiesLocalization.Title(family);

        View.CurrentObjectChanged += View_CurrentObjectChanged;
        UpdateActionState();
    }

    protected override void OnDeactivated()
    {
        if (View != null)
            View.CurrentObjectChanged -= View_CurrentObjectChanged;

        base.OnDeactivated();
    }

    private void View_CurrentObjectChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState() =>
        viewDocumentCopiesAction.Enabled["Object"] = CanOpen(View?.CurrentObject);

    private void ViewDocumentCopiesAction_Execute(object sender, SimpleActionExecuteEventArgs e) =>
        HeaderDocumentCopiesOpenHelper.TryOpenFromViewObject(Application, View, View.CurrentObject);

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

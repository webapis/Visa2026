using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;

namespace Visa2026.Module.Controllers;

/// <summary>
/// Issue a new <see cref="Visa"/> from an unused <see cref="InvitationItem"/> line on an issued invitation.
/// </summary>
public sealed class InvitationItemIssueVisaController : ViewController
{
    private readonly SimpleAction _issueVisaAction;

    public InvitationItemIssueVisaController()
    {
        TargetObjectType = typeof(InvitationItem);

        _issueVisaAction = new SimpleAction(this, "InvitationItemIssueVisa", PredefinedCategory.RecordEdit)
        {
            Caption = "Issue visa",
            ImageName = "BO_Visa",
            SelectionDependencyType = SelectionDependencyType.RequireSingleObject,
            ToolTip = VisaUiMessages.Get("InvitationItem.IssueVisa.ToolTip"),
        };
        _issueVisaAction.Execute += OnIssueVisa;
    }

    protected override void OnActivated()
    {
        base.OnActivated();
        UpdateActionState();
        View.CurrentObjectChanged += OnViewContextChanged;
        View.SelectionChanged += OnViewContextChanged;
    }

    protected override void OnDeactivated()
    {
        View.CurrentObjectChanged -= OnViewContextChanged;
        View.SelectionChanged -= OnViewContextChanged;
        base.OnDeactivated();
    }

    private void OnViewContextChanged(object? sender, EventArgs e) => UpdateActionState();

    private void UpdateActionState()
    {
        var item = View.CurrentObject as InvitationItem;
        var canIssue = VisaFromInvitationItemHelper.CanIssueVisaFromInvitationItem(
            item,
            View.ObjectSpace,
            out _);
        _issueVisaAction.Active.SetItemValue(nameof(InvitationItemIssueVisaController), canIssue);
        _issueVisaAction.Enabled.SetItemValue(nameof(InvitationItemIssueVisaController), canIssue);
    }

    private void OnIssueVisa(object sender, SimpleActionExecuteEventArgs e)
    {
        if (View.CurrentObject is not InvitationItem item || item.ID == Guid.Empty)
            return;

        if (!VisaFromInvitationItemHelper.TryOpenCreateVisa(
                Application,
                Frame,
                item.ID,
                _issueVisaAction,
                out var blockMessageKey))
        {
            var message = blockMessageKey != null
                ? VisaUiMessages.Get(blockMessageKey)
                : VisaUiMessages.Get("InvitationItem.IssueVisa.NotAvailable");
            Application.ShowViewStrategy.ShowMessage(message, InformationType.Warning);
        }
    }
}

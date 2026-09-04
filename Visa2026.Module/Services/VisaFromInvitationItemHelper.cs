using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Opens a new <see cref="Visa"/> from an unused <see cref="InvitationItem"/> (invitation-item-centric create).
/// Stamps <see cref="Visa.IssuingApplicationProfileInstance"/> from the parent invitation and pre-selects the line.
/// </summary>
public static class VisaFromInvitationItemHelper
{
    public static bool CanIssueVisaFromInvitationItem(
        InvitationItem? invitationItem,
        IObjectSpace? objectSpace,
        out string? blockMessageKey)
    {
        blockMessageKey = null;

        if (invitationItem == null)
        {
            blockMessageKey = "InvitationItem.IssueVisa.NotAvailable";
            return false;
        }

        if (invitationItem.IsCancelled || invitationItem.IsChanged || invitationItem.IsUsed)
        {
            blockMessageKey = "InvitationItem.IssueVisa.ItemUsedOrClosed";
            return false;
        }

        if (objectSpace != null)
        {
            var itemId = invitationItem.ID;
            if (objectSpace.GetObjectsQuery<Visa>()
                .Any(v => v.IssuingInvitationItem != null && v.IssuingInvitationItem.ID == itemId))
            {
                blockMessageKey = "InvitationItem.IssueVisa.VisaAlreadyIssued";
                return false;
            }
        }
        else if (invitationItem.IssuedVisa != null)
        {
            blockMessageKey = "InvitationItem.IssueVisa.VisaAlreadyIssued";
            return false;
        }

        var invitation = invitationItem.Invitation;
        var instance = invitation?.ApplicationProfileInstance;
        if (instance == null)
        {
            blockMessageKey = "InvitationItem.IssueVisa.NoIssuingInstance";
            return false;
        }

        if (!VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(instance)
            || !VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(instance))
        {
            blockMessageKey = "InvitationItem.IssueVisa.IneligibleInstance";
            return false;
        }

        if (invitationItem.Person == null || invitationItem.Passport == null)
        {
            blockMessageKey = "InvitationItem.IssueVisa.NoPassport";
            return false;
        }

        return true;
    }

    public static bool TryOpenCreateVisa(
        XafApplication application,
        Frame? sourceFrame,
        Guid invitationItemId,
        ActionBase? showViewSourceAction,
        out string? blockMessageKey)
    {
        blockMessageKey = null;

        if (application == null || invitationItemId == Guid.Empty)
        {
            blockMessageKey = "InvitationItem.IssueVisa.NotAvailable";
            return false;
        }

        var probeSpace = application.CreateObjectSpace(typeof(InvitationItem));
        try
        {
            var sourceItem = probeSpace.GetObjectByKey<InvitationItem>(invitationItemId);
            if (!CanIssueVisaFromInvitationItem(sourceItem, probeSpace, out blockMessageKey))
                return false;
        }
        finally
        {
            probeSpace.Dispose();
        }

        var visaObjectSpace = application.CreateObjectSpace(typeof(Visa));
        var invitationItem = visaObjectSpace.GetObjectByKey<InvitationItem>(invitationItemId);
        if (invitationItem?.Invitation?.ApplicationProfileInstance == null)
        {
            visaObjectSpace.Dispose();
            blockMessageKey = "InvitationItem.IssueVisa.NoIssuingInstance";
            return false;
        }

        var issuingInstance = visaObjectSpace.GetObject(invitationItem.Invitation.ApplicationProfileInstance);
        var passport = invitationItem.Passport != null
            ? visaObjectSpace.GetObject(invitationItem.Passport)
            : null;

        var visa = visaObjectSpace.CreateObject<Visa>();
        visa.IssuingApplicationProfileInstance = issuingInstance;
        visa.IssuingInvitationItem = invitationItem;
        visa.Passport = passport;
        visa.PathAIssuingLinksApplied = true;

        var detailView = application.CreateDetailView(visaObjectSpace, visa);
        detailView.ViewEditMode = ViewEditMode.Edit;

        application.ShowViewStrategy.ShowView(
            new ShowViewParameters(detailView) { TargetWindow = TargetWindow.NewModalWindow },
            new ShowViewSource(sourceFrame, showViewSourceAction));

        return true;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Mutual exclusivity and sibling alignment for <see cref="InvitationItem"/> status flags
/// (<see cref="InvitationItem.IsCancelled"/>, <see cref="InvitationItem.IsChanged"/>, <see cref="InvitationItem.IsUsed"/>).
/// </summary>
public static class InvitationStatusFlagsHelper
{
    public static bool AreInvitationItemFlagsExclusive(bool isCancelled, bool isChanged, bool isUsed) =>
        CountSetFlags(isCancelled, isChanged, isUsed) <= 1;

    public static void NormalizeInvitationItem(InvitationItem item)
    {
        int setCount = CountSetFlags(item.IsCancelled, item.IsChanged, item.IsUsed);
        if (setCount <= 1)
        {
            return;
        }

        if (item.IsCancelled)
        {
            item.SetItemStatusFlags(cancelled: true, changed: false, used: false);
        }
        else if (item.IsChanged)
        {
            item.SetItemStatusFlags(cancelled: false, changed: true, used: false);
        }
        else if (item.IsUsed)
        {
            item.SetItemStatusFlags(cancelled: false, changed: false, used: true);
        }
    }

    /// <summary>
    /// When one item is marked cancelled or changed, apply the same status to every active sibling on the invitation.
    /// </summary>
    public static void AlignSiblingsFromItem(InvitationItem source)
    {
        Invitation? invitation = source.Invitation;
        if (invitation == null)
        {
            return;
        }

        if (source.IsCancelled)
        {
            ApplyStatusToAllItems(invitation, cancelled: true, changed: false, used: false);
        }
        else if (source.IsChanged)
        {
            ApplyStatusToAllItems(invitation, cancelled: false, changed: true, used: false);
        }
    }

    private static void ApplyStatusToAllItems(Invitation invitation, bool cancelled, bool changed, bool used) =>
        ForEachActiveItem(invitation, item => item.SetItemStatusFlags(cancelled, changed, used));

    private static void ForEachActiveItem(Invitation invitation, Action<InvitationItem> update)
    {
        IObjectSpace? objectSpace = ObjectSpaceHelper.Get(invitation);
        foreach (InvitationItem item in GetActiveInvitationItems(invitation, objectSpace))
        {
            InvitationItem target = objectSpace != null ? objectSpace.GetObject(item) : item;
            update(target);
            objectSpace?.SetModified(target);
        }
    }

    private static IReadOnlyList<InvitationItem> GetActiveInvitationItems(Invitation invitation, IObjectSpace? objectSpace)
    {
        if (objectSpace != null && invitation.ID != Guid.Empty)
        {
            Guid invitationId = invitation.ID;
            return objectSpace.GetObjectsQuery<InvitationItem>()
                .Where(i => i.Invitation != null && i.Invitation.ID == invitationId && !i.IsDeleted)
                .ToList();
        }

        if (invitation.InvitationItems == null)
        {
            return Array.Empty<InvitationItem>();
        }

        return invitation.InvitationItems.Where(i => !i.IsDeleted).ToList();
    }

    private static int CountSetFlags(params bool[] flags) =>
        flags.Count(static f => f);
}

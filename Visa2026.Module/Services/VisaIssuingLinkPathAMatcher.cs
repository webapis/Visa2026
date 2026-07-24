using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Path A (manual UI create): resolve <see cref="Visa.IssuingApplicationItem"/> and optional
/// <see cref="Visa.InvitationItem"/> once for a new Visa. Not used by legacy import (Path B).
/// </summary>
public static class VisaIssuingLinkPathAMatcher
{
    /// <summary>
    /// Applies Path A defaults at most once while <paramref name="visa"/> is still a new object.
    /// Skips during <see cref="MigrationImportContext.IsDataImport"/>.
    /// </summary>
    public static void TryApplyOnce(Visa visa)
    {
        if (visa == null || visa.PathAIssuingLinksApplied)
            return;

        if (MigrationImportContext.IsDataImport)
            return;

        var objectSpace = ObjectSpaceHelper.Get(visa);
        if (objectSpace == null || !objectSpace.IsNewObject(visa))
            return;

        var person = visa.Passport?.Person;
        if (person == null)
            return;

        // Mark attempted even when no candidate — do not re-run on later edits.
        visa.PathAIssuingLinksApplied = true;

        var issuingItem = FindIssuingApplicationItem(objectSpace, visa, person.ID);
        if (issuingItem == null)
            return;

        visa.IssuingApplicationItem = issuingItem;

        var applicationType = issuingItem.Application?.ApplicationType;
        if (!ApplicationTypeCapabilities.CanIssueInvitation(applicationType))
        {
            visa.InvitationItem = null;
            return;
        }

        var invitationItem = FindInvitationItem(objectSpace, visa, person.ID, issuingItem.Application);
        if (invitationItem == null)
            return;

        visa.InvitationItem = invitationItem;
        if (!invitationItem.IsUsed)
            invitationItem.SetItemStatusFlags(cancelled: false, changed: false, used: true);
    }

    private static ApplicationItem? FindIssuingApplicationItem(IObjectSpace objectSpace, Visa visa, Guid personId)
    {
        var visaId = visa.ID;
        var issueDate = visa.IssueDate;

        var linkedIssuingIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId && v.IssuingApplicationItem != null)
            .Select(v => v.IssuingApplicationItem!.ID)
            .ToList();

        var cancelledCode = ApplicationProgressStateCodes.ProcessCancelled;

        var query = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai => ai.Person != null && ai.Person.ID == personId)
            .Where(ai => ai.Application != null && ai.Application.ApplicationType != null)
            .Where(ai =>
                ai.Application.ApplicationType.CanIssueVisa
                || ai.Application.ApplicationType.CanIssueInvitation)
            .Where(ai => !ai.InvitationItemIsCancelled)
            .Where(ai => !ai.VisaIsCancelled)
            .Where(ai => !ai.IsCancelled)
            .Where(ai => !ai.ApplicationItemsIsCancelled)
            .Where(ai =>
                ai.Application.LatestPrimaryStateCode == null
                || ai.Application.LatestPrimaryStateCode != cancelledCode);

        if (linkedIssuingIds.Count > 0)
            query = query.Where(ai => !linkedIssuingIds.Contains(ai.ID));

        // Materialize then apply date filter — EF date comparisons vary by provider.
        var candidates = query
            .OrderByDescending(ai => ai.Application!.ApplicationDate)
            .ThenByDescending(ai => ai.Application!.ID)
            .ToList();

        if (issueDate != default)
        {
            candidates = candidates
                .Where(ai => ai.Application!.ApplicationDate.Date < issueDate.Date)
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        // Extension / transfer: prefer the line whose CurrentVisa is the preceding visa on this passport.
        var predecessor = FindPredecessorVisa(objectSpace, visa);
        if (predecessor != null)
        {
            var viaPredecessor = candidates
                .Where(ai => ai.CurrentVisa != null && ai.CurrentVisa.ID == predecessor.ID)
                .ToList();
            if (viaPredecessor.Count > 0)
                return viaPredecessor[0];
        }

        // Prefer an invitation-issuing app that already has an unused invitation line for this person
        // (avoids picking a newer App_Inv that is still in progress with no invitation yet).
        var withUnusedInvitation = candidates
            .Where(ai => ApplicationTypeCapabilities.CanIssueInvitation(ai.Application?.ApplicationType)
                && HasUnusedInvitationItem(objectSpace, ai.Application!, personId, visaId))
            .ToList();

        if (withUnusedInvitation.Count > 0)
            return withUnusedInvitation[0];

        return candidates[0];
    }

    /// <summary>
    /// Latest other visa on the same passport issued before this visa (or latest other when IssueDate unset).
    /// </summary>
    private static Visa? FindPredecessorVisa(IObjectSpace objectSpace, Visa visa)
    {
        var passportId = visa.Passport?.ID;
        if (passportId == null || passportId == Guid.Empty)
            return null;

        var visaId = visa.ID;
        var others = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId && v.Passport != null && v.Passport.ID == passportId)
            .ToList();

        if (others.Count == 0)
            return null;

        if (visa.IssueDate != default)
        {
            others = others
                .Where(v => v.IssueDate != default && v.IssueDate.Date < visa.IssueDate.Date)
                .ToList();
        }

        return others
            .OrderByDescending(v => v.IssueDate)
            .ThenByDescending(v => v.ID)
            .FirstOrDefault();
    }

    private static bool HasUnusedInvitationItem(
        IObjectSpace objectSpace,
        Application application,
        Guid personId,
        Guid visaId)
    {
        var applicationId = application.ID;
        var linkedInvitationIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId && v.InvitationItem != null)
            .Select(v => v.InvitationItem!.ID)
            .ToList();

        var query = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(ii => ii.Person != null && ii.Person.ID == personId)
            .Where(ii => ii.Invitation != null
                && ii.Invitation.Application != null
                && ii.Invitation.Application.ID == applicationId)
            .Where(ii => !ii.IsCancelled && !ii.IsChanged && !ii.IsUsed);

        if (linkedInvitationIds.Count > 0)
            query = query.Where(ii => !linkedInvitationIds.Contains(ii.ID));

        return query.Any();
    }

    private static InvitationItem? FindInvitationItem(
        IObjectSpace objectSpace,
        Visa visa,
        Guid personId,
        Application? issuingApplication)
    {
        if (issuingApplication == null)
            return null;

        var visaId = visa.ID;
        var issueDate = visa.IssueDate;
        var applicationId = issuingApplication.ID;
        var applicationDate = issuingApplication.ApplicationDate.Date;

        var linkedInvitationIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId && v.InvitationItem != null)
            .Select(v => v.InvitationItem!.ID)
            .ToList();

        var query = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(ii => ii.Person != null && ii.Person.ID == personId)
            .Where(ii => ii.Invitation != null)
            .Where(ii => ii.Invitation.Application != null && ii.Invitation.Application.ID == applicationId)
            .Where(ii => !ii.IsCancelled && !ii.IsChanged && !ii.IsUsed);

        if (linkedInvitationIds.Count > 0)
            query = query.Where(ii => !linkedInvitationIds.Contains(ii.ID));

        var candidates = query
            .OrderByDescending(ii => ii.Invitation!.IssuedDate)
            .ThenByDescending(ii => ii.Invitation!.ID)
            .AsEnumerable()
            .Where(ii => ii.Invitation!.IssuedDate.Date > applicationDate)
            .ToList();

        if (issueDate != default)
        {
            candidates = candidates
                .Where(ii => ii.Invitation!.IssuedDate.Date < issueDate.Date)
                .ToList();
        }

        return candidates.FirstOrDefault();
    }
}
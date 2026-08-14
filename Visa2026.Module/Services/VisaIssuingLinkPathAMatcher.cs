using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Path A (manual UI create): resolve <see cref="Visa.IssuingApplicationProfileInstance"/>
/// plus optional <see cref="Visa.InvitationItem"/>. Not used by legacy import (Path B).
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

        visa.PathAIssuingLinksApplied = true;

        var candidate = FindIssuingApplicationProfileInstanceCandidate(objectSpace, visa, person.ID);
        if (candidate == null)
            return;

        visa.IssuingApplicationProfileInstance = candidate;

        if (!VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForVisa(visa))
        {
            visa.InvitationItem = null;
            return;
        }

        var invitationItem = FindInvitationItem(objectSpace, visa, person.ID, candidate);
        if (invitationItem == null)
            return;

        visa.InvitationItem = invitationItem;
        if (!invitationItem.IsUsed)
            invitationItem.SetItemStatusFlags(cancelled: false, changed: false, used: true);
    }

    private static ApplicationProfileInstance? FindIssuingApplicationProfileInstanceCandidate(
        IObjectSpace objectSpace,
        Visa visa,
        Guid personId)
    {
        var visaId = visa.ID;
        var issueDate = visa.IssueDate;
        var cancelledCode = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled;

        var linkedApplicationProfileInstanceIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId)
            .AsEnumerable()
            .Select(v => VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(v)?.ID ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        var candidates = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.People.Any(p => p.ID == personId))
            .AsEnumerable()
            .Where(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance)
            .Where(app => app.LatestPrimaryStateCode == null
                || app.LatestPrimaryStateCode != cancelledCode)
            .GroupBy(app => app.ID)
            .Select(g => g.First())
            .Where(app => !linkedApplicationProfileInstanceIds.Contains(app.ID))
            .OrderByDescending(app => app.ApplicationDate)
            .ThenByDescending(app => app.ID)
            .ToList();

        if (issueDate != default)
        {
            candidates = candidates
                .Where(app => app.ApplicationDate.Date < issueDate.Date)
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        var predecessor = FindPredecessorVisa(objectSpace, visa);
        if (predecessor != null)
        {
            var viaPredecessor = candidates
                .Where(app => ReferencesPredecessorVisa(objectSpace, app, personId, predecessor))
                .ToList();
            if (viaPredecessor.Count > 0)
                return viaPredecessor[0];
        }

        var withUnusedInvitation = candidates
            .Where(app => VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(app)
                && HasUnusedInvitationItem(objectSpace, app, personId, visaId))
            .ToList();

        if (withUnusedInvitation.Count > 0)
            return withUnusedInvitation[0];

        return candidates[0];
    }

    private static bool ReferencesPredecessorVisa(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Guid personId,
        Visa predecessor)
    {
        return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Any(link =>
                link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == personId
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.Visa
                && link.LinkedObjectId == predecessor.ID);
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
        ApplicationProfileInstance application,
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
                && ii.Invitation.ApplicationProfileInstance != null
                && ii.Invitation.ApplicationProfileInstance.ID == applicationId)
            .Where(ii => !ii.IsCancelled && !ii.IsChanged && !ii.IsUsed);

        if (linkedInvitationIds.Count > 0)
            query = query.Where(ii => !linkedInvitationIds.Contains(ii.ID));

        return query.Any();
    }

    private static InvitationItem? FindInvitationItem(
        IObjectSpace objectSpace,
        Visa visa,
        Guid personId,
        ApplicationProfileInstance? issuingApplication)
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
            .Where(ii => ii.Invitation.ApplicationProfileInstance != null && ii.Invitation.ApplicationProfileInstance.ID == applicationId)
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

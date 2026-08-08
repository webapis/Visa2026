using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Path A (manual UI create): resolve <see cref="Visa.IssuingApplication"/> (and legacy
/// <see cref="Visa.IssuingApplicationItem"/> when present) plus optional <see cref="Visa.InvitationItem"/>.
/// Not used by legacy import (Path B).
/// </summary>
public static class VisaIssuingLinkPathAMatcher
{
    private sealed record IssuingCandidate(Application Application, ApplicationItem? LegacyItem);

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

        var candidate = FindIssuingApplicationCandidate(objectSpace, visa, person.ID);
        if (candidate?.Application == null)
            return;

        visa.IssuingApplication = candidate.Application;
        if (candidate.LegacyItem != null)
            visa.IssuingApplicationItem = candidate.LegacyItem;

        if (!VisaIssuingApplicationHelper.CanIssueInvitationForVisa(visa))
        {
            visa.InvitationItem = null;
            return;
        }

        var invitationItem = FindInvitationItem(objectSpace, visa, person.ID, candidate.Application);
        if (invitationItem == null)
            return;

        visa.InvitationItem = invitationItem;
        if (!invitationItem.IsUsed)
            invitationItem.SetItemStatusFlags(cancelled: false, changed: false, used: true);
    }

    private static IssuingCandidate? FindIssuingApplicationCandidate(
        IObjectSpace objectSpace,
        Visa visa,
        Guid personId)
    {
        var visaId = visa.ID;
        var issueDate = visa.IssueDate;
        var cancelledCode = ApplicationProgressStateCodes.ProcessCancelled;

        var linkedApplicationIds = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.ID != visaId)
            .AsEnumerable()
            .Select(v => VisaIssuingApplicationHelper.GetEffectiveIssuingApplication(v)?.ID ?? Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        var m2mApplications = objectSpace.GetObjectsQuery<ApplicationPerson>()
            .Where(ap => ap.PersonId == personId && ap.Application != null)
            .Select(ap => ap.Application!)
            .AsEnumerable()
            .Where(VisaIssuingApplicationHelper.IsEligibleIssuingApplication)
            .Where(app => app.LatestPrimaryStateCode == null
                || app.LatestPrimaryStateCode != cancelledCode)
            .GroupBy(app => app.ID)
            .Select(g => g.First())
            .ToList();

        var m2mAppIds = m2mApplications.Select(a => a.ID).ToHashSet();

        var legacyItems = objectSpace.GetObjectsQuery<ApplicationItem>()
            .Where(ai => ai.Person != null && ai.Person.ID == personId)
            .Where(ai => ai.Application != null)
            .Where(ai => !ai.InvitationItemIsCancelled)
            .Where(ai => !ai.VisaIsCancelled)
            .Where(ai => !ai.IsCancelled)
            .Where(ai => !ai.ApplicationItemsIsCancelled)
            .Where(ai =>
                ai.Application!.LatestPrimaryStateCode == null
                || ai.Application.LatestPrimaryStateCode != cancelledCode)
            .AsEnumerable()
            .Where(ai => VisaIssuingApplicationHelper.IsEligibleIssuingApplication(ai.Application))
            .Where(ai => !m2mAppIds.Contains(ai.Application!.ID))
            .ToList();

        var candidates = m2mApplications
            .Select(app => new IssuingCandidate(app, null))
            .Concat(legacyItems.Select(item => new IssuingCandidate(item.Application!, item)))
            .Where(c => !linkedApplicationIds.Contains(c.Application.ID))
            .OrderByDescending(c => c.Application.ApplicationDate)
            .ThenByDescending(c => c.Application.ID)
            .ToList();

        if (issueDate != default)
        {
            candidates = candidates
                .Where(c => c.Application.ApplicationDate.Date < issueDate.Date)
                .ToList();
        }

        if (candidates.Count == 0)
            return null;

        var predecessor = FindPredecessorVisa(objectSpace, visa);
        if (predecessor != null)
        {
            var viaPredecessor = candidates
                .Where(c => ReferencesPredecessorVisa(objectSpace, c, personId, predecessor))
                .ToList();
            if (viaPredecessor.Count > 0)
                return viaPredecessor[0];
        }

        var withUnusedInvitation = candidates
            .Where(c => VisaIssuingApplicationHelper.CanIssueInvitationForApplication(c.Application)
                && HasUnusedInvitationItem(objectSpace, c.Application, personId, visaId))
            .ToList();

        if (withUnusedInvitation.Count > 0)
            return withUnusedInvitation[0];

        return candidates[0];
    }

    private static bool ReferencesPredecessorVisa(
        IObjectSpace objectSpace,
        IssuingCandidate candidate,
        Guid personId,
        Visa predecessor)
    {
        if (candidate.LegacyItem?.CurrentVisa != null
            && candidate.LegacyItem.CurrentVisa.ID == predecessor.ID)
        {
            return true;
        }

        var applicationPerson = objectSpace.GetObjectsQuery<ApplicationPerson>()
            .FirstOrDefault(ap => ap.ApplicationId == candidate.Application.ID && ap.PersonId == personId);

        return applicationPerson?.ResolvedLinks?
            .Any(link => link.LinkKind == ApplicationPersonLinkKind.Visa
                && link.LinkedObjectId == predecessor.ID) == true;
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

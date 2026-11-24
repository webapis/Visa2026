using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Renewed / changed / cancelled invitation (and cancelled WP) blocks on Ýagdaý syny.
/// </summary>
public static partial class ApplicationProfileInstanceStatePreview
{
    public const string RelatedSuccessorTitleKey =
        "ApplicationProfileInstance.List.Mark.InvitationSuccessorTitle";

    public const string RelatedCancelledInvitationTitleKey =
        "ApplicationProfileInstance.StatePreview.Section.CancelledInvitation";

    public const string RelatedCancelledWorkPermitTitleKey =
        "ApplicationProfileInstance.StatePreview.Section.CancelledWorkPermit";

    public static bool IsGroupedSection(SectionId id) =>
        id is SectionId.Invitation
            or SectionId.WorkPermit
            or SectionId.RenewedInvitations
            or SectionId.ChangedInvitation
            or SectionId.CancelledInvitation
            or SectionId.CancelledWorkPermit;

    public static bool PeriodOnHeaderFor(SectionId id) =>
        id is not SectionId.WorkPermit and not SectionId.CancelledWorkPermit;

    public static bool StrikeNameForLife(string? life) =>
        string.Equals(life, InvitationLaterLife.Changed, StringComparison.Ordinal)
        || string.Equals(life, InvitationLaterLife.Cancelled, StringComparison.Ordinal);

    /// <summary>Append renewed + look-back sections after the profile's normal document sections.</summary>
    public static void AppendRelatedSections(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        List<Section> sections)
    {
        var renewed = BuildRenewedInvitationSection(objectSpace, sections);
        if (renewed != null)
            sections.Add(renewed);

        var code = application.ApplicationProfile?.Code?.Trim();
        if (string.Equals(code, "change_invitation", StringComparison.OrdinalIgnoreCase))
        {
            var lookBack = BuildLookBackInvitationSection(
                objectSpace,
                application,
                SectionId.ChangedInvitation,
                ApplicationProfileActionFamily.Change);
            if (lookBack != null)
                sections.Add(lookBack);
            return;
        }

        if (string.Equals(code, "cancel_invitation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "cancel_invitation_wp", StringComparison.OrdinalIgnoreCase))
        {
            var invitationLookBack = BuildLookBackInvitationSection(
                objectSpace,
                application,
                SectionId.CancelledInvitation,
                ApplicationProfileActionFamily.Cancellation);
            if (invitationLookBack != null)
                sections.Add(invitationLookBack);
        }

        if (string.Equals(code, "cancel_invitation_wp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(code, "cancel_workpermit", StringComparison.OrdinalIgnoreCase))
        {
            var wpLookBack = BuildLookBackWorkPermitSection(objectSpace, application);
            if (wpLookBack != null)
                sections.Add(wpLookBack);
        }
    }

    public static Dictionary<Guid, ApplicationListLinkedPeople.RelatedApplicationLink> LoadCancellationLinks(
        IObjectSpace objectSpace,
        Guid oldInstanceId,
        IReadOnlyCollection<Guid> invitationItemIds)
    {
        var result = new Dictionary<Guid, ApplicationListLinkedPeople.RelatedApplicationLink>();
        if (oldInstanceId == Guid.Empty || invitationItemIds.Count == 0)
            return result;

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        var family = ApplicationProfileActionFamily.Cancellation;
        var links = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApplicationProfile != null
                && a.ApplicationProfile.ActionFamily == family
                && a.LatestPrimaryStateCode == issued
                && a.InvitationItems.Any(i =>
                    invitationItemIds.Contains(i.ID)
                    && i.Person != null
                    && i.Invitation != null
                    && i.Invitation.ApplicationProfileInstance != null
                    && i.Invitation.ApplicationProfileInstance.ID == oldInstanceId))
            .SelectMany(a => a.InvitationItems
                .Where(i => invitationItemIds.Contains(i.ID)
                    && i.Person != null
                    && i.Invitation != null
                    && i.Invitation.ApplicationProfileInstance != null
                    && i.Invitation.ApplicationProfileInstance.ID == oldInstanceId)
                .Select(i => new
                {
                    CancelId = a.ID,
                    Number = a.FullApplicationNumber,
                    Date = a.ApplicationDate,
                    PersonId = i.Person.ID,
                }))
            .ToList();

        var grouped = new Dictionary<Guid, List<(DateTime Date, Guid ApplicationId, string? Number)>>();
        foreach (var link in links)
        {
            if (!grouped.TryGetValue(link.PersonId, out var options))
            {
                options = [];
                grouped[link.PersonId] = options;
            }

            options.Add((link.Date, link.CancelId, link.Number));
        }

        foreach (var (personId, options) in grouped)
        {
            var chosen = ApplicationListLinkedPeople.ChooseRelatedApplication(options);
            if (chosen != null)
                result[personId] = chosen;
        }

        return result;
    }

    public readonly record struct WorkPermitCancellations(
        HashSet<Guid> ItemIds,
        Dictionary<Guid, ApplicationListLinkedPeople.RelatedApplicationLink> ByPerson);

    /// <summary>
    /// Same permit number, ignoring spaces. A cancellation may link a duplicate row of the same permit.
    /// </summary>
    public static bool SameWorkPermitHeader(string? left, string? right)
    {
        var a = (left ?? string.Empty).Trim();
        var b = (right ?? string.Empty).Trim();
        return a.Length > 0 && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    public static bool WorkPermitItemIsCancelledByLink(
        Guid sheetItemId,
        Guid sheetPersonId,
        string? sheetHeaderNumber,
        Guid linkedItemId,
        Guid linkedPersonId,
        string? linkedHeaderNumber) =>
        linkedItemId == sheetItemId
        || (sheetPersonId != Guid.Empty
            && sheetPersonId == linkedPersonId
            && SameWorkPermitHeader(sheetHeaderNumber, linkedHeaderNumber));

    public static WorkPermitCancellations LoadWorkPermitCancellations(
        IObjectSpace objectSpace,
        IReadOnlyCollection<Guid> workPermitItemIds)
    {
        var itemIds = new HashSet<Guid>();
        var byPerson = new Dictionary<Guid, ApplicationListLinkedPeople.RelatedApplicationLink>();
        if (workPermitItemIds.Count == 0)
            return new WorkPermitCancellations(itemIds, byPerson);

        var sheet = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(item => workPermitItemIds.Contains(item.ID) && item.Person != null)
            .Select(item => new
            {
                item.ID,
                PersonId = item.Person.ID,
                HeaderNumber = item.WorkPermit != null ? item.WorkPermit.WorkPermitNumber : null,
            })
            .ToList();
        if (sheet.Count == 0)
            return new WorkPermitCancellations(itemIds, byPerson);

        var personIds = sheet.Select(row => row.PersonId).Distinct().ToList();
        var family = ApplicationProfileActionFamily.Cancellation;
        // In progress is enough. Match the linked row or another row of the same permit number.
        var links = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(application => application.ApplicationProfile != null
                && application.ApplicationProfile.ActionFamily == family
                && application.WorkPermitItems.Any(item =>
                    item.Person != null && personIds.Contains(item.Person.ID)))
            .SelectMany(application => application.WorkPermitItems
                .Where(item => item.Person != null && personIds.Contains(item.Person.ID))
                .Select(item => new
                {
                    CancelId = application.ID,
                    Number = application.FullApplicationNumber,
                    Date = application.ApplicationDate,
                    PersonId = item.Person.ID,
                    ItemId = item.ID,
                    HeaderNumber = item.WorkPermit != null ? item.WorkPermit.WorkPermitNumber : null,
                }))
            .ToList();

        var grouped = new Dictionary<Guid, List<(DateTime Date, Guid ApplicationId, string? Number)>>();
        foreach (var row in sheet)
        {
            foreach (var link in links)
            {
                if (!WorkPermitItemIsCancelledByLink(
                        row.ID,
                        row.PersonId,
                        row.HeaderNumber,
                        link.ItemId,
                        link.PersonId,
                        link.HeaderNumber))
                    continue;

                itemIds.Add(row.ID);
                if (!grouped.TryGetValue(row.PersonId, out var options))
                {
                    options = [];
                    grouped[row.PersonId] = options;
                }

                options.Add((link.Date, link.CancelId, link.Number));
            }
        }

        foreach (var (personId, options) in grouped)
        {
            var chosen = ApplicationListLinkedPeople.ChooseRelatedApplication(options);
            if (chosen != null)
                byPerson[personId] = chosen;
        }

        return new WorkPermitCancellations(itemIds, byPerson);
    }

    private static Section? BuildRenewedInvitationSection(
        IObjectSpace objectSpace,
        IReadOnlyList<Section> sections)
    {
        var invitation = sections.FirstOrDefault(section => section.Id == SectionId.Invitation);
        if (invitation?.Groups == null || invitation.Groups.Count == 0)
            return null;

        var byDocument = new Dictionary<Guid, RenewedGroupBuilder>();
        var order = new List<RenewedGroupBuilder>();
        foreach (var group in invitation.Groups)
        {
            foreach (var person in group.People)
            {
                if (person.SuccessorDocumentId == Guid.Empty)
                    continue;

                if (!byDocument.TryGetValue(person.SuccessorDocumentId, out var builder))
                {
                    builder = new RenewedGroupBuilder(
                        person.SuccessorDocumentId,
                        person.SuccessorInvitationNumber,
                        person.SuccessorNumber,
                        person.SuccessorApplicationId,
                        person.SuccessorLifeKey);
                    byDocument[person.SuccessorDocumentId] = builder;
                    order.Add(builder);
                }

                var life = string.IsNullOrEmpty(person.SuccessorLifeKey)
                    ? InvitationLaterLife.InForce
                    : person.SuccessorLifeKey;
                var (stateKey, tone) = Life(life);
                builder.People.Add(new GroupPerson(
                    person.PersonName,
                    string.Empty,
                    stateKey,
                    tone,
                    Array.Empty<Row>(),
                    StrikeName: false));
            }
        }

        if (order.Count == 0)
            return null;

        var documentIds = order.Select(group => group.DocumentId).ToList();
        var headers = objectSpace.GetObjectsQuery<Invitation>()
            .Where(invitationHeader => documentIds.Contains(invitationHeader.ID))
            .Select(invitationHeader => new
            {
                invitationHeader.ID,
                invitationHeader.InvitationNumber,
                invitationHeader.IssuedDate,
                invitationHeader.ExpirationDate,
            })
            .ToList()
            .ToDictionary(header => header.ID);

        var groups = new List<HeaderGroup>(order.Count);
        foreach (var builder in order.OrderBy(group => group.InvitationNumber, StringComparer.CurrentCultureIgnoreCase))
        {
            headers.TryGetValue(builder.DocumentId, out var header);
            var number = !string.IsNullOrWhiteSpace(builder.InvitationNumber)
                ? builder.InvitationNumber!
                : Text(header?.InvitationNumber);
            var period = header == null
                ? "—"
                : FormatPeriod(header.IssuedDate, header.ExpirationDate);
            groups.Add(new HeaderGroup(
                number,
                period,
                PeriodOnHeader: true,
                builder.DocumentId,
                HeaderDocumentCopiesFamily.Invitation,
                Guid.Empty,
                string.IsNullOrEmpty(builder.LifeKey) ? InvitationLaterLife.InForce : builder.LifeKey,
                builder.People
                    .OrderBy(person => person.PersonName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                builder.ApplicationNumber,
                builder.ApplicationId));
        }

        return new Section(SectionId.RenewedInvitations, false, false, Array.Empty<Row>(), groups, null);
    }

    private static Section? BuildLookBackInvitationSection(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        SectionId sectionId,
        ApplicationProfileActionFamily family)
    {
        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        if (!string.Equals(application.LatestPrimaryStateCode, issued, StringComparison.OrdinalIgnoreCase)
            && application.Invitations?.Count > 0 == false)
        {
            // Still show look-back when linked items exist even if not issued yet? Prototype shows completed cases.
        }

        var rows = objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => item.ApplicationProfileInstances.Any(instance => instance.ID == application.ID)
                && item.Person != null
                && item.Invitation != null
                && item.Invitation.ApplicationProfileInstance != null
                && item.Invitation.ApplicationProfileInstance.ID != application.ID)
            .Select(item => new
            {
                PersonId = item.Person.ID,
                FirstName = item.Person.FirstName,
                MiddleName = item.Person.MiddleName,
                LastName = item.Person.LastName,
                DocumentId = item.Invitation.ID,
                InvitationNumber = item.Invitation.InvitationNumber,
                IssuedDate = item.Invitation.IssuedDate,
                ExpirationDate = item.Invitation.ExpirationDate,
                OldInstanceId = item.Invitation.ApplicationProfileInstance.ID,
                OldNumber = item.Invitation.ApplicationProfileInstance.FullApplicationNumber,
                ItemId = item.ID,
            })
            .ToList();
        if (rows.Count == 0)
            return null;

        var itemIds = rows.Select(row => row.ItemId).Distinct().ToList();
        var cancelled = CompletedInvitationItemIds(objectSpace, itemIds, ApplicationProfileActionFamily.Cancellation);
        var changed = CompletedInvitationItemIds(objectSpace, itemIds, ApplicationProfileActionFamily.Change);
        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, itemIds);

        var lifeKey = family == ApplicationProfileActionFamily.Change
            ? InvitationLaterLife.Changed
            : InvitationLaterLife.Cancelled;
        var (stateKey, tone) = Life(lifeKey);

        var byDocument = new Dictionary<Guid, LookBackGroupBuilder>();
        var order = new List<LookBackGroupBuilder>();
        foreach (var row in rows)
        {
            var usedNow = used.Contains(row.ItemId);
            // Keep the look-back life as changed/cancelled for the acted-on document.
            _ = InvitationLaterLife.Resolve(
                cancelled.Contains(row.ItemId),
                changed.Contains(row.ItemId),
                usedNow,
                InvitationLaterLife.IsExpired(row.ExpirationDate));

            if (!byDocument.TryGetValue(row.DocumentId, out var builder))
            {
                builder = new LookBackGroupBuilder(
                    row.DocumentId,
                    Text(row.InvitationNumber),
                    FormatPeriod(row.IssuedDate, row.ExpirationDate),
                    row.OldNumber?.Trim() ?? string.Empty,
                    row.OldInstanceId,
                    lifeKey);
                byDocument[row.DocumentId] = builder;
                order.Add(builder);
            }

            var name = ApplicationListLinkedPeople.FormatName(row.FirstName, row.MiddleName, row.LastName);
            builder.People.Add(new GroupPerson(
                name,
                string.Empty,
                stateKey,
                tone,
                Array.Empty<Row>(),
                SuccessorPersonId: row.PersonId,
                StrikeName: true));
        }

        var groups = order
            .OrderBy(group => group.Number, StringComparer.CurrentCultureIgnoreCase)
            .Select(builder => new HeaderGroup(
                builder.Number,
                builder.Period,
                PeriodOnHeader: true,
                builder.DocumentId,
                HeaderDocumentCopiesFamily.Invitation,
                Guid.Empty,
                builder.LifeKey,
                builder.People
                    .OrderBy(person => person.PersonName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                builder.ApplicationNumber,
                builder.ApplicationId))
            .ToList();

        return new Section(sectionId, false, false, Array.Empty<Row>(), groups, null);
    }

    private static Section? BuildLookBackWorkPermitSection(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application)
    {
        var rows = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(item => item.ApplicationProfileInstances.Any(instance => instance.ID == application.ID)
                && item.Person != null
                && item.WorkPermit != null
                && item.WorkPermit.ApplicationProfileInstance != null
                && item.WorkPermit.ApplicationProfileInstance.ID != application.ID)
            .Select(item => new
            {
                PersonId = item.Person.ID,
                FirstName = item.Person.FirstName,
                MiddleName = item.Person.MiddleName,
                LastName = item.Person.LastName,
                DocumentId = item.WorkPermit.ID,
                Number = item.WorkPermit.WorkPermitNumber,
                Start = item.StartDate,
                End = item.ExpirationDate,
                OldInstanceId = item.WorkPermit.ApplicationProfileInstance.ID,
                OldNumber = item.WorkPermit.ApplicationProfileInstance.FullApplicationNumber,
            })
            .ToList();
        if (rows.Count == 0)
            return null;

        var (stateKey, tone) = Life(InvitationLaterLife.Cancelled);
        var byDocument = new Dictionary<Guid, LookBackGroupBuilder>();
        var order = new List<LookBackGroupBuilder>();
        foreach (var row in rows)
        {
            if (!byDocument.TryGetValue(row.DocumentId, out var builder))
            {
                builder = new LookBackGroupBuilder(
                    row.DocumentId,
                    Text(row.Number),
                    FormatPeriod(row.Start, row.End),
                    row.OldNumber?.Trim() ?? string.Empty,
                    row.OldInstanceId,
                    InvitationLaterLife.Cancelled);
                byDocument[row.DocumentId] = builder;
                order.Add(builder);
            }

            var name = ApplicationListLinkedPeople.FormatName(row.FirstName, row.MiddleName, row.LastName);
            builder.People.Add(new GroupPerson(
                name,
                FormatPeriod(row.Start, row.End),
                stateKey,
                tone,
                Array.Empty<Row>(),
                StrikeName: true));
        }

        var groups = order
            .OrderBy(group => group.Number, StringComparer.CurrentCultureIgnoreCase)
            .Select(builder => new HeaderGroup(
                builder.Number,
                builder.Period,
                PeriodOnHeader: false,
                builder.DocumentId,
                HeaderDocumentCopiesFamily.WorkPermit,
                Guid.Empty,
                builder.LifeKey,
                builder.People
                    .OrderBy(person => person.PersonName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                builder.ApplicationNumber,
                builder.ApplicationId))
            .ToList();

        return new Section(SectionId.CancelledWorkPermit, false, false, Array.Empty<Row>(), groups, null);
    }

    private static HashSet<Guid> CompletedInvitationItemIds(
        IObjectSpace objectSpace,
        IReadOnlyCollection<Guid> itemIds,
        ApplicationProfileActionFamily family)
    {
        if (itemIds.Count == 0)
            return [];

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => itemIds.Contains(item.ID)
                && item.ApplicationProfileInstances.Any(instance =>
                    instance.ApplicationProfile != null
                    && instance.ApplicationProfile.ActionFamily == family
                    && instance.LatestPrimaryStateCode == issued))
            .Select(item => item.ID)
            .ToHashSet();
    }

    private sealed class RenewedGroupBuilder
    {
        public RenewedGroupBuilder(
            Guid documentId,
            string? invitationNumber,
            string? applicationNumber,
            Guid applicationId,
            string? lifeKey)
        {
            DocumentId = documentId;
            InvitationNumber = invitationNumber;
            ApplicationNumber = applicationNumber;
            ApplicationId = applicationId;
            LifeKey = lifeKey;
        }

        public Guid DocumentId { get; }
        public string? InvitationNumber { get; }
        public string? ApplicationNumber { get; }
        public Guid ApplicationId { get; }
        public string? LifeKey { get; }
        public List<GroupPerson> People { get; } = new();
    }

    private sealed class LookBackGroupBuilder
    {
        public LookBackGroupBuilder(
            Guid documentId,
            string number,
            string period,
            string applicationNumber,
            Guid applicationId,
            string? lifeKey)
        {
            DocumentId = documentId;
            Number = number;
            Period = period;
            ApplicationNumber = applicationNumber;
            ApplicationId = applicationId;
            LifeKey = lifeKey;
        }

        public Guid DocumentId { get; }
        public string Number { get; }
        public string Period { get; }
        public string ApplicationNumber { get; }
        public Guid ApplicationId { get; }
        public string? LifeKey { get; }
        public List<GroupPerson> People { get; } = new();
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

public static partial class PersonLinkReview
{
    private static IReadOnlyList<PersonLinkReviewLine> LastNLines(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        string label)
    {
        var lastCount = ApplicationProfilePersonLastCount.For(application, kind);
        if (lastCount <= 0)
            return Array.Empty<PersonLinkReviewLine>();

        var entities = ResolveLastNEntities(objectSpace, application, person, kind, lastCount);
        if (entities.Count == 0)
        {
            var empty = Line(objectSpace, kind, label, person);
            empty.SlotIndex = 0;
            empty.SlotCount = 0;
            return new[] { empty };
        }

        var total = entities.Count;
        var rows = new List<PersonLinkReviewLine>(total);
        for (var i = 0; i < total; i++)
        {
            var line = LineFromEntity(objectSpace, kind, label, person, entities[i]);
            line.SlotIndex = i + 1;
            line.SlotCount = total;
            rows.Add(line);
        }

        return rows;
    }

    private static IReadOnlyList<object> ResolveLastNEntities(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        int lastCount) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Passport =>
            ApplicationProfileInstancePersonValidItems.ResolvePassports(objectSpace, person, lastCount).Cast<object>().ToList(),
        ApplicationProfileInstancePersonLinkKind.Visa =>
            ApplicationProfileInstancePersonValidItems.ResolveVisas(
                objectSpace,
                person,
                lastCount,
                includeExpired: ApplicationProfileInstancePersonValidItems.LinksLastIssuedVisaIncludingExpired(application)).Cast<object>().ToList(),
        ApplicationProfileInstancePersonLinkKind.InvitationItem =>
            ApplicationProfileInstancePersonValidItems.ResolveInvitationItems(objectSpace, person, lastCount).Cast<object>().ToList(),
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem =>
            ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItems(objectSpace, person, lastCount).Cast<object>().ToList(),
        ApplicationProfileInstancePersonLinkKind.BorderZoneItem =>
            ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItems(objectSpace, person, lastCount).Cast<object>().ToList(),
        _ => Array.Empty<object>(),
    };

    private static PersonLinkReviewLine LineFromEntity(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        string label,
        Person person,
        object entity)
    {
        var (summary, id) = entity switch
        {
            Passport passport => PassportSummary(passport),
            Visa visa => VisaSummary(visa),
            InvitationItem invitation => TextId(invitation.InvitationItemName, invitation.ID),
            WorkPermitItem workPermit => WorkPermitSummary(workPermit),
            BorderZoneItem borderZone => TextId(borderZone.Passport?.PassportNumber, borderZone.ID),
            _ => ("—", (Guid?)Guid.Empty),
        };

        var line = new PersonLinkReviewLine
        {
            Kind = kind,
            Label = label,
            Summary = string.IsNullOrWhiteSpace(summary) ? "—" : summary,
            ObjectId = id ?? Guid.Empty,
        };
        Attach(line, ChoicesFor(objectSpace, kind, person.ID));
        return line;
    }

    private static (string Summary, Guid? Id) WorkPermitSummary(WorkPermitItem? item) =>
        (JoinParts(item?.WorkPermitNumber, DateText(item?.StartDate), DateText(item?.ExpirationDate)), item?.ID);

    private static IReadOnlyList<Guid> OrderPinnedLikeResolve(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        IReadOnlyList<Guid> pinnedIds)
    {
        var lastCount = ApplicationProfilePersonLastCount.For(application, kind);
        var resolvedOrder = ResolveLastNEntities(objectSpace, application, person, kind, Math.Max(lastCount, pinnedIds.Count))
            .Select(entity => entity switch
            {
                BaseObject bo => bo.ID,
                _ => Guid.Empty,
            })
            .Where(id => id != Guid.Empty)
            .ToList();

        var ordered = resolvedOrder.Where(pinnedIds.Contains).ToList();
        foreach (var id in pinnedIds)
        {
            if (!ordered.Contains(id))
                ordered.Add(id);
        }

        return ordered;
    }
}
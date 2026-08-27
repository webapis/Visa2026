using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

internal static class ApplicationWorkspaceTabBuilder
{
    public static IReadOnlyList<ApplicationWorkspaceTab> Build(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfile? profile)
    {
        var people = application.People?
            .Where(p => p != null)
            .OrderBy(p => p!.LastName)
            .ThenBy(p => p!.FirstName)
            .Cast<Person>()
            .ToList() ?? [];

        var linksByPerson = LoadLinksByPerson(objectSpace, application.ID);
        var linkedEntities = LoadLinkedEntities(objectSpace, application, linksByPerson);

        return
        [
            PersonTab(people),
            PassportTab(application, people, linksByPerson, linkedEntities),
            VisaTab(application, people, linksByPerson, linkedEntities),
            EducationTab(application, people, linksByPerson, linkedEntities),
            AddressTab(application, people, linksByPerson, linkedEntities),
            WorkPermitTab(application, people, linksByPerson, linkedEntities),
            InvitationTab(application, people, linksByPerson, linkedEntities),
            BorderZoneTab(application, people, linksByPerson, linkedEntities),
            PositionTab(application, people, linksByPerson, linkedEntities),
            SalaryTab(application, people, linksByPerson, linkedEntities),
            MedicalTab(application, people, linksByPerson, linkedEntities),
            TravelTab(application, people, linksByPerson, linkedEntities),
            RejectionTab(application, people, linksByPerson, linkedEntities),
        ];
    }

    private static Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> LoadLinksByPerson(
        IObjectSpace objectSpace,
        Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return [];

        return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == applicationId)
            .ToList()
            .GroupBy(l => l.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> LoadLinkedEntities(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson)
    {
        var map = new Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object>();
        if (objectSpace == null || linksByPerson.Count == 0)
            return map;

        var idsByKind = new Dictionary<ApplicationProfileInstancePersonLinkKind, HashSet<Guid>>();
        foreach (var links in linksByPerson.Values)
        {
            foreach (var link in links)
            {
                if (link.LinkKind is not { } kind
                    || link.LinkedObjectId is not Guid id
                    || id == Guid.Empty)
                    continue;
                if (!ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, kind))
                    continue;

                if (!idsByKind.TryGetValue(kind, out var ids))
                {
                    ids = [];
                    idsByKind[kind] = ids;
                }

                ids.Add(id);
            }
        }

        void AddRange<T>(ApplicationProfileInstancePersonLinkKind kind, IEnumerable<T> entities)
            where T : BaseObject
        {
            foreach (var entity in entities)
            {
                if (entity != null)
                    map[(kind, entity.ID)] = entity;
            }
        }

        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.Passport, out var passportIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.Passport,
                objectSpace.GetObjectsQuery<Passport>().Where(p => passportIds.Contains(p.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.Visa, out var visaIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.Visa,
                objectSpace.GetObjectsQuery<Visa>().Include(v => v.VisaType).Where(v => visaIds.Contains(v.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.Education, out var educationIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.Education,
                objectSpace.GetObjectsQuery<Education>()
                    .Include(e => e.EducationInstitution)
                    .Include(e => e.EducationLevel)
                    .Where(e => educationIds.Contains(e.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.AddressOfResidence, out var addressIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                objectSpace.GetObjectsQuery<AddressOfResidence>().Include(a => a.City).Where(a => addressIds.Contains(a.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.WorkPermitItem, out var wpIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
                objectSpace.GetObjectsQuery<WorkPermitItem>().Where(w => wpIds.Contains(w.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.InvitationItem, out var invIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.InvitationItem,
                objectSpace.GetObjectsQuery<InvitationItem>().Include(i => i.Invitation).Where(i => invIds.Contains(i.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.BorderZoneItem, out var bzIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.BorderZoneItem,
                objectSpace.GetObjectsQuery<BorderZoneItem>().Include(b => b.BorderZone).Where(b => bzIds.Contains(b.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.Position, out var positionIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.Position,
                objectSpace.GetObjectsQuery<EmployeePositionHistory>().Include(p => p.Position).Where(p => positionIds.Contains(p.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.Salary, out var salaryIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.Salary,
                objectSpace.GetObjectsQuery<EmployeeSalary>().Where(s => salaryIds.Contains(s.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.MedicalRecord, out var medicalIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.MedicalRecord,
                objectSpace.GetObjectsQuery<MedicalRecord>().Where(m => medicalIds.Contains(m.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.TravelHistory, out var travelIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.TravelHistory,
                objectSpace.GetObjectsQuery<TravelHistory>()
                    .Include(t => t.CheckPoint)
                    .Include(t => t.City)
                    .Where(t => travelIds.Contains(t.ID)).ToList());
        if (idsByKind.TryGetValue(ApplicationProfileInstancePersonLinkKind.RejectionItem, out var rejectionIds))
            AddRange(ApplicationProfileInstancePersonLinkKind.RejectionItem,
                objectSpace.GetObjectsQuery<RejectionItem>().Include(r => r.Rejection).Where(r => rejectionIds.Contains(r.ID)).ToList());

        return map;
    }

    private static ApplicationWorkspaceTab PersonTab(IReadOnlyList<Person> people)
    {
        var rowList = people.Select(person =>
        {
            return new[]
            {
                person?.FullName ?? "—",
                person?.PersonRole.ToString() ?? "—",
                person?.PersonalNumber ?? "—",
                "ApplicationProfileInstancePeople",
            };
        }).ToList();

        var personIds = people.Select(p => p.ID).ToList();
        return new ApplicationWorkspaceTab
        {
            Key = "person",
            Label = "Person",
            Visible = true,
            Columns = ["Person", "Role", "Personal №", "Source"],
            Rows = rowList,
            RowPersonIds = personIds,
            RowApplicationProfileInstancePersonIds = personIds,
            EmptyMessage = rowList.Count == 0
                ? "No people linked — use Link existing… below or Link person on the toolbar."
                : null,
            SqlViewHint = "vw_application_workspace_person",
        };
    }

    private static ApplicationWorkspaceTab PassportTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("passport", "Passport", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Passport),
            ["Person", "Passport №", "Issued", "Expires"],
            RowsForKind<Passport>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.Passport, (person, passport) =>
            [
                PersonName(person),
                passport.PassportNumber ?? "—",
                Fmt(passport.IssueDate),
                Fmt(passport.ExpirationDate),
            ]),
            emptyMessage: "No valid passport linked.");

    private static ApplicationWorkspaceTab VisaTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("visa", "Visa", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Visa),
            ["Person", "Visa №", "Type", "Valid to"],
            RowsForKind<Visa>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.Visa, (person, visa) =>
            [
                PersonName(person),
                visa.VisaNumber ?? "—",
                visa.VisaType?.LocalizedDisplayName ?? visa.VisaType?.NameTm ?? "—",
                Fmt(visa.ExpirationDate),
            ]),
            emptyMessage: "No valid visa linked.");

    private static ApplicationWorkspaceTab EducationTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("education", "Education", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Education),
            ["Person", "Institution", "Level", "Year"],
            RowsForKind<Education>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.Education, (person, edu) =>
            [
                PersonName(person),
                edu.EducationInstitution?.LocalizedDisplayName ?? edu.EducationInstitution?.NameTm ?? "—",
                edu.EducationLevel?.LocalizedDisplayName ?? edu.EducationLevel?.NameTm ?? "—",
                edu.GraduationYear ?? "—",
            ]),
            emptyMessage: "No education record linked.");

    private static ApplicationWorkspaceTab AddressTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("address", "Address", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.AddressOfResidence),
            ["Person", "City", "Address"],
            RowsForKind<AddressOfResidence>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.AddressOfResidence, (person, addr) =>
            [
                PersonName(person),
                addr.City?.NameTm ?? "—",
                addr.FullAddress ?? "—",
            ]),
            emptyMessage: "No address linked.");

    private static ApplicationWorkspaceTab WorkPermitTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("wp", "Work permit", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.WorkPermitItem),
            ["Person", "WP item", "Location", "Valid to"],
            RowsForKind<WorkPermitItem>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.WorkPermitItem, (person, wp) =>
            [
                PersonName(person),
                wp.WorkPermitNumber ?? "—",
                wp.WorkPermittedLocations ?? "—",
                Fmt(wp.ExpirationDate),
            ]),
            emptyMessage: "No work permit item linked.");

    private static ApplicationWorkspaceTab InvitationTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("inv", "Invitation", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.InvitationItem),
            ["Person", "Invitation item", "Status"],
            RowsForKind<InvitationItem>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.InvitationItem, (person, inv) =>
            [
                PersonName(person),
                inv.Invitation?.InvitationNumber ?? inv.ID.ToString(),
                inv.Invitation != null ? "Current" : "—",
            ]),
            emptyMessage: "No invitation item linked.");

    private static ApplicationWorkspaceTab BorderZoneTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("bz", "Border zone", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.BorderZoneItem),
            ["Person", "Border zone item", "Number"],
            RowsForKind<BorderZoneItem>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.BorderZoneItem, (person, bz) =>
            [
                PersonName(person),
                bz.BorderZone?.BorderZoneNumber ?? bz.ID.ToString(),
                bz.BorderZone?.BorderZoneNumber ?? "—",
            ]),
            emptyMessage: "No border zone item linked.");

    private static ApplicationWorkspaceTab PositionTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("position", "Position", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Position),
            ["Person", "Position", "From"],
            RowsForKind<EmployeePositionHistory>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.Position, (person, pos) =>
            [
                PersonName(person),
                pos.Position?.NameTm ?? "—",
                Fmt(pos.StartDate),
            ]),
            emptyMessage: "No position linked.");

    private static ApplicationWorkspaceTab SalaryTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("salary", "Salary", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Salary),
            ["Person", "Amount", "Currency"],
            RowsForKind<EmployeeSalary>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.Salary, (person, sal) =>
            [
                PersonName(person),
                sal.Amount ?? "—",
                sal.Currency?.ToString() ?? "—",
            ]),
            emptyMessage: "No salary linked.");

    private static ApplicationWorkspaceTab MedicalTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("medical", "Medical", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.MedicalRecord),
            ["Person", "Record", "Valid to"],
            RowsForKind<MedicalRecord>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.MedicalRecord, (person, med) =>
            [
                PersonName(person),
                med.DocumentNumber ?? med.ID.ToString(),
                Fmt(med.ExpirationDate),
            ]),
            emptyMessage: "No medical record linked.");

    private static ApplicationWorkspaceTab TravelTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("travel", "Travel history", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.TravelHistory),
            ["Person", "Kind", "Date", "Check point"],
            RowsForKind<TravelHistory>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.TravelHistory, (person, th) =>
            [
                PersonName(person),
                th.MovementType?.ToString() ?? th.TravelType?.ToString() ?? "—",
                Fmt(th.TravelDate),
                th.CheckPoint?.NameTm ?? th.City?.NameTm ?? "—",
            ]),
            emptyMessage: "No travel history linked.");

    private static ApplicationWorkspaceTab RejectionTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities) =>
        Tab("rejection", "Rejection", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.RejectionItem),
            ["Person", "Rejection item", "Status"],
            RowsForKind<RejectionItem>(people, linksByPerson, linkedEntities, ApplicationProfileInstancePersonLinkKind.RejectionItem, (person, rej) =>
            [
                PersonName(person),
                rej.Rejection?.RejectedDocNumber ?? rej.ID.ToString(),
                rej.Rejection != null ? "Current" : "—",
            ]),
            emptyMessage: "No rejection item linked.");

    private readonly record struct KindRows(
        IReadOnlyList<IReadOnlyList<string>> Rows,
        IReadOnlyList<Guid> PersonIds);

    private static KindRows RowsForKind<T>(
        IReadOnlyList<Person> people,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
        Dictionary<(ApplicationProfileInstancePersonLinkKind Kind, Guid Id), object> linkedEntities,
        ApplicationProfileInstancePersonLinkKind kind,
        Func<Person, T, IReadOnlyList<string>> map)
        where T : class
    {
        var rows = new List<IReadOnlyList<string>>();
        var personIds = new List<Guid>();
        foreach (var person in people)
        {
            if (person == null || !linksByPerson.TryGetValue(person.ID, out var links))
                continue;

            foreach (var link in links.Where(l => l.LinkKind == kind))
            {
                if (link?.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                    continue;

                if (!linkedEntities.TryGetValue((kind, linkedId), out var entityObj) || entityObj is not T entity)
                    continue;

                rows.Add(map(person, entity));
                personIds.Add(person.ID);
            }
        }

        return new KindRows(rows, personIds);
    }

    private static ApplicationWorkspaceTab Tab(
        string key,
        string label,
        bool visible,
        IReadOnlyList<string> columns,
        KindRows rows,
        string? emptyMessage = null,
        string? sqlViewHint = null)
    {
        return new ApplicationWorkspaceTab
        {
            Key = key,
            Label = label,
            Visible = visible,
            Columns = columns,
            Rows = rows.Rows,
            RowPersonIds = rows.PersonIds,
            EmptyMessage = rows.Rows.Count == 0 ? emptyMessage : null,
            SqlViewHint = sqlViewHint,
        };
    }

    private static string PersonName(Person? person) => person?.FullName ?? "—";

    private static string Fmt(DateTime? date) =>
        date.HasValue && date.Value != default
            ? date.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : "—";

    private static string Fmt(DateTime date) =>
        date == default ? "—" : date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
}

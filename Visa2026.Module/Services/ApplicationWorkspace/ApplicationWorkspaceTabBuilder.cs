using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
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

        return
        [
            PersonTab(people),
            PassportTab(application, people, objectSpace, linksByPerson),
            VisaTab(application, people, objectSpace, linksByPerson),
            EducationTab(application, people, objectSpace, linksByPerson),
            AddressTab(application, people, objectSpace, linksByPerson),
            WorkPermitTab(application, people, objectSpace, linksByPerson),
            InvitationTab(application, people, objectSpace, linksByPerson),
            BorderZoneTab(application, people, objectSpace, linksByPerson),
            PositionTab(application, people, objectSpace, linksByPerson),
            SalaryTab(application, people, objectSpace, linksByPerson),
            MedicalTab(application, people, objectSpace, linksByPerson),
            TravelTab(application, people, objectSpace, linksByPerson),
            RejectionTab(application, people, objectSpace, linksByPerson),
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("passport", "Passport", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Passport),
            ["Person", "Passport №", "Issued", "Expires"],
            RowsForKind<Passport>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.Passport, (person, passport) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("visa", "Visa", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Visa),
            ["Person", "Visa №", "Type", "Valid to"],
            RowsForKind<Visa>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.Visa, (person, visa) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("education", "Education", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Education),
            ["Person", "Institution", "Level", "Year"],
            RowsForKind<Education>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.Education, (person, edu) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("address", "Address", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.AddressOfResidence),
            ["Person", "City", "Address"],
            RowsForKind<AddressOfResidence>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.AddressOfResidence, (person, addr) =>
            [
                PersonName(person),
                addr.City?.NameTm ?? "—",
                addr.FullAddress ?? "—",
            ]),
            emptyMessage: "No address linked.");

    private static ApplicationWorkspaceTab WorkPermitTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("wp", "Work permit", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.WorkPermitItem),
            ["Person", "WP item", "Location", "Valid to"],
            RowsForKind<WorkPermitItem>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.WorkPermitItem, (person, wp) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("inv", "Invitation", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.InvitationItem),
            ["Person", "Invitation item", "Status"],
            RowsForKind<InvitationItem>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.InvitationItem, (person, inv) =>
            [
                PersonName(person),
                inv.Invitation?.InvitationNumber ?? inv.ID.ToString(),
                inv.Invitation != null ? "Current" : "—",
            ]),
            emptyMessage: "No invitation item linked.");

    private static ApplicationWorkspaceTab BorderZoneTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("bz", "Border zone", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.BorderZoneItem),
            ["Person", "Border zone item", "Number"],
            RowsForKind<BorderZoneItem>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.BorderZoneItem, (person, bz) =>
            [
                PersonName(person),
                bz.BorderZone?.BorderZoneNumber ?? bz.ID.ToString(),
                bz.BorderZone?.BorderZoneNumber ?? "—",
            ]),
            emptyMessage: "No border zone item linked.");

    private static ApplicationWorkspaceTab PositionTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("position", "Position", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Position),
            ["Person", "Position", "From"],
            RowsForKind<EmployeePositionHistory>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.Position, (person, pos) =>
            [
                PersonName(person),
                pos.Position?.NameTm ?? "—",
                Fmt(pos.StartDate),
            ]),
            emptyMessage: "No position linked.");

    private static ApplicationWorkspaceTab SalaryTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("salary", "Salary", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.Salary),
            ["Person", "Amount", "Currency"],
            RowsForKind<EmployeeSalary>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.Salary, (person, sal) =>
            [
                PersonName(person),
                sal.Amount ?? "—",
                sal.Currency?.ToString() ?? "—",
            ]),
            emptyMessage: "No salary linked.");

    private static ApplicationWorkspaceTab MedicalTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("medical", "Medical", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.MedicalRecord),
            ["Person", "Record", "Valid to"],
            RowsForKind<MedicalRecord>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.MedicalRecord, (person, med) =>
            [
                PersonName(person),
                med.DocumentNumber ?? med.ID.ToString(),
                Fmt(med.ExpirationDate),
            ]),
            emptyMessage: "No medical record linked.");

    private static ApplicationWorkspaceTab TravelTab(
        ApplicationProfileInstance application,
        IReadOnlyList<Person> people,
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("travel", "Travel history", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.TravelHistory),
            ["Person", "Kind", "Date", "Check point"],
            RowsForKind<TravelHistory>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.TravelHistory, (person, th) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson) =>
        Tab("rejection", "Rejection", ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, ApplicationProfileInstancePersonLinkKind.RejectionItem),
            ["Person", "Rejection item", "Status"],
            RowsForKind<RejectionItem>(people, objectSpace, linksByPerson, ApplicationProfileInstancePersonLinkKind.RejectionItem, (person, rej) =>
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
        IObjectSpace objectSpace,
        Dictionary<Guid, List<ApplicationProfileInstancePersonResolvedLink>> linksByPerson,
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

            var link = links.FirstOrDefault(l => l.LinkKind == kind);
            if (link?.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                continue;

            var entity = objectSpace.GetObjectByKey<T>(linkedId);
            if (entity == null)
                continue;

            rows.Add(map(person, entity));
            personIds.Add(person.ID);
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

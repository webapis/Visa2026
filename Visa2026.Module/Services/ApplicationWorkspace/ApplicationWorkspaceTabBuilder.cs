using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationWorkspace;

internal static class ApplicationWorkspaceTabBuilder
{
    public static IReadOnlyList<ApplicationWorkspaceTab> Build(
        IObjectSpace objectSpace,
        Application application,
        ApplicationProfile? profile)
    {
        var people = application.People?
            .OrderBy(p => p.LinkedAt)
            .ToList() ?? [];

        return
        [
            PersonTab(people),
            PassportTab(profile, people, objectSpace),
            VisaTab(profile, people, objectSpace),
            EducationTab(profile, people, objectSpace),
            AddressTab(profile, people, objectSpace),
            WorkPermitTab(profile, people, objectSpace),
            InvitationTab(profile, people, objectSpace),
            BorderZoneTab(profile, people, objectSpace),
            PositionTab(profile, people, objectSpace),
            SalaryTab(profile, people, objectSpace),
            MedicalTab(profile, people, objectSpace),
            TravelTab(profile, people, objectSpace),
            RejectionTab(profile, people, objectSpace),
        ];
    }

    private static ApplicationWorkspaceTab PersonTab(IReadOnlyList<ApplicationPerson> people)
    {
        var rowList = people.Select(ap =>
        {
            var person = ap.Person;
            return new[]
            {
                person?.FullName ?? "—",
                person?.PersonRole.ToString() ?? "—",
                person?.PersonalNumber ?? "—",
                "ApplicationPeople",
            };
        }).ToList();

        return new ApplicationWorkspaceTab
        {
            Key = "person",
            Label = "Person",
            Visible = true,
            Columns = ["Person", "Role", "Personal №", "Source"],
            Rows = rowList,
            RowPersonIds = people.Select(ap => ap.PersonId).ToList(),
            RowApplicationPersonIds = people.Select(ap => ap.ID).ToList(),
            EmptyMessage = rowList.Count == 0
                ? "No people linked — use Link existing… below or Link person on the toolbar."
                : null,
            SqlViewHint = "vw_application_workspace_person",
        };
    }

    private static ApplicationWorkspaceTab PassportTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("passport", "Passport", profile?.RequirePersonPassport ?? true,
            ["Person", "Passport №", "Issued", "Expires"],
            RowsForKind<Passport>(people, objectSpace, ApplicationPersonLinkKind.Passport, (ap, passport) =>
            [
                PersonName(ap.Person),
                passport.PassportNumber ?? "—",
                Fmt(passport.IssueDate),
                Fmt(passport.ExpirationDate),
            ]),
            emptyMessage: "No valid passport linked.");

    private static ApplicationWorkspaceTab VisaTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("visa", "Visa", profile?.RequirePersonVisa ?? false,
            ["Person", "Visa №", "Type", "Valid to"],
            RowsForKind<Visa>(people, objectSpace, ApplicationPersonLinkKind.Visa, (ap, visa) =>
            [
                PersonName(ap.Person),
                visa.VisaNumber ?? "—",
                visa.VisaType?.LocalizedDisplayName ?? visa.VisaType?.NameTm ?? "—",
                Fmt(visa.ExpirationDate),
            ]),
            emptyMessage: "No valid visa linked.");

    private static ApplicationWorkspaceTab EducationTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("education", "Education", profile?.RequirePersonEducation ?? false,
            ["Person", "Institution", "Level", "Year"],
            RowsForKind<Education>(people, objectSpace, ApplicationPersonLinkKind.Education, (ap, edu) =>
            [
                PersonName(ap.Person),
                edu.EducationInstitution?.LocalizedDisplayName ?? edu.EducationInstitution?.NameTm ?? "—",
                edu.EducationLevel?.LocalizedDisplayName ?? edu.EducationLevel?.NameTm ?? "—",
                edu.GraduationYear ?? "—",
            ]),
            emptyMessage: "No education record linked.");

    private static ApplicationWorkspaceTab AddressTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("address", "Address", profile?.RequirePersonAddressOfResidence ?? false,
            ["Person", "City", "Address"],
            RowsForKind<AddressOfResidence>(people, objectSpace, ApplicationPersonLinkKind.AddressOfResidence, (ap, addr) =>
            [
                PersonName(ap.Person),
                addr.City?.NameTm ?? "—",
                addr.FullAddress ?? "—",
            ]),
            emptyMessage: "No address linked.");

    private static ApplicationWorkspaceTab WorkPermitTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("wp", "Work permit", profile?.RequirePersonWorkPermitItem ?? false,
            ["Person", "WP item", "Location", "Valid to"],
            RowsForKind<WorkPermitItem>(people, objectSpace, ApplicationPersonLinkKind.WorkPermitItem, (ap, wp) =>
            [
                PersonName(ap.Person),
                wp.WorkPermitNumber ?? "—",
                wp.WorkPermittedLocations ?? "—",
                Fmt(wp.ExpirationDate),
            ]),
            emptyMessage: "No work permit item linked.");

    private static ApplicationWorkspaceTab InvitationTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("inv", "Invitation", profile?.RequirePersonInvitationItem ?? false,
            ["Person", "Invitation item", "Status"],
            RowsForKind<InvitationItem>(people, objectSpace, ApplicationPersonLinkKind.InvitationItem, (ap, inv) =>
            [
                PersonName(ap.Person),
                inv.Invitation?.InvitationNumber ?? inv.ID.ToString(),
                inv.Invitation != null ? "Current" : "—",
            ]),
            emptyMessage: "No invitation item linked.");

    private static ApplicationWorkspaceTab BorderZoneTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("bz", "Border zone", profile?.RequirePersonBorderZoneItem ?? false,
            ["Person", "Border zone item", "Number"],
            RowsForKind<BorderZoneItem>(people, objectSpace, ApplicationPersonLinkKind.BorderZoneItem, (ap, bz) =>
            [
                PersonName(ap.Person),
                bz.BorderZone?.BorderZoneNumber ?? bz.ID.ToString(),
                bz.BorderZone?.BorderZoneNumber ?? "—",
            ]),
            emptyMessage: "No border zone item linked.");

    private static ApplicationWorkspaceTab PositionTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("position", "Position", profile?.RequirePersonPosition ?? false,
            ["Person", "Position", "From"],
            RowsForKind<EmployeePositionHistory>(people, objectSpace, ApplicationPersonLinkKind.Position, (ap, pos) =>
            [
                PersonName(ap.Person),
                pos.Position?.NameTm ?? "—",
                Fmt(pos.StartDate),
            ]),
            emptyMessage: "No position linked.");

    private static ApplicationWorkspaceTab SalaryTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("salary", "Salary", profile?.RequirePersonSalary ?? false,
            ["Person", "Amount", "Currency"],
            RowsForKind<EmployeeSalary>(people, objectSpace, ApplicationPersonLinkKind.Salary, (ap, sal) =>
            [
                PersonName(ap.Person),
                sal.Amount ?? "—",
                sal.Currency?.ToString() ?? "—",
            ]),
            emptyMessage: "No salary linked.");

    private static ApplicationWorkspaceTab MedicalTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("medical", "Medical", profile?.RequirePersonMedical ?? false,
            ["Person", "Record", "Valid to"],
            RowsForKind<MedicalRecord>(people, objectSpace, ApplicationPersonLinkKind.MedicalRecord, (ap, med) =>
            [
                PersonName(ap.Person),
                med.DocumentNumber ?? med.ID.ToString(),
                Fmt(med.ExpirationDate),
            ]),
            emptyMessage: "No medical record linked.");

    private static ApplicationWorkspaceTab TravelTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("travel", "Travel history", profile?.RequirePersonTravelHistory ?? false,
            ["Person", "Kind", "Date", "Check point"],
            RowsForKind<TravelHistory>(people, objectSpace, ApplicationPersonLinkKind.TravelHistory, (ap, th) =>
            [
                PersonName(ap.Person),
                th.MovementType?.ToString() ?? th.TravelType?.ToString() ?? "—",
                Fmt(th.TravelDate),
                th.CheckPoint?.NameTm ?? th.City?.NameTm ?? "—",
            ]),
            emptyMessage: "No travel history linked.");

    private static ApplicationWorkspaceTab RejectionTab(
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace) =>
        Tab("rejection", "Rejection", profile?.RequirePersonRejectionItem ?? false,
            ["Person", "Rejection item", "Status"],
            RowsForKind<RejectionItem>(people, objectSpace, ApplicationPersonLinkKind.RejectionItem, (ap, rej) =>
            [
                PersonName(ap.Person),
                rej.Rejection?.RejectedDocNumber ?? rej.ID.ToString(),
                rej.Rejection != null ? "Current" : "—",
            ]),
            emptyMessage: "No rejection item linked.");

    private static IEnumerable<IReadOnlyList<string>> RowsForKind<T>(
        IReadOnlyList<ApplicationPerson> people,
        IObjectSpace objectSpace,
        ApplicationPersonLinkKind kind,
        Func<ApplicationPerson, T, IReadOnlyList<string>> map)
        where T : class
    {
        foreach (var ap in people)
        {
            var link = ap.ResolvedLinks?.FirstOrDefault(l => l.LinkKind == kind);
            if (link?.LinkedObjectId is not Guid linkedId || linkedId == Guid.Empty)
                continue;

            var entity = objectSpace.GetObjectByKey<T>(linkedId);
            if (entity == null)
                continue;

            yield return map(ap, entity);
        }
    }

    private static ApplicationWorkspaceTab Tab(
        string key,
        string label,
        bool visible,
        IReadOnlyList<string> columns,
        IEnumerable<IReadOnlyList<string>> rows,
        string? emptyMessage = null,
        string? sqlViewHint = null)
    {
        var rowList = rows.ToList();
        return new ApplicationWorkspaceTab
        {
            Key = key,
            Label = label,
            Visible = visible,
            Columns = columns,
            Rows = rowList,
            EmptyMessage = rowList.Count == 0 ? emptyMessage : null,
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

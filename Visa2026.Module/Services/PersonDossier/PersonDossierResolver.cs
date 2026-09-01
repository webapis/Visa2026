using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Builds the read-only dossier snapshot for one <see cref="Person"/>.
/// </summary>
/// <remarks>
/// Mirrors the shape of <c>PersonLinkedDocumentsResolver</c> (sections -> records) so a dossier row
/// can be aligned with its document-copies row by <c>RecordKey</c>.
/// </remarks>
public static class PersonDossierResolver
{
    private const string CurrentDateFormat = "dd MMM yyyy";

    private const int ExpiringSoonDays = 30;
    private const int ExpiringWatchDays = 90;

    public static PersonDossierSnapshot Resolve(IObjectSpace objectSpace, Person? person)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        if (person == null)
            return new PersonDossierSnapshot();

        var target = objectSpace.GetObject(person) ?? person;

        var sections = BuildSections(target)
            .Where(section => section.Records.Count > 0)
            .OrderBy(section => section.SortOrder)
            .ToList();

        return new PersonDossierSnapshot
        {
            PersonId = ResolveId(objectSpace, target) ?? Guid.Empty,
            PersonDisplayName = target.FullName ?? string.Empty,
            PersonalNumber = target.PersonalNumber,
            PersonRole = target.PersonRole,
            PersonRoleLabel = L($"Role.{target.PersonRole}"),
            ProjectContractName = Describe(target.ProjectContract),
            PhotoDataUri = BuildPhotoDataUri(target.Photo),
            IsArchived = target.IsArchived,
            IdentityFields = BuildIdentityFields(target),
            StatusTiles = BuildStatusTiles(target),
            Sections = sections,
        };
    }

    // ---------------------------------------------------------------- identity

    private static IReadOnlyList<PersonDossierField> BuildIdentityFields(Person person)
    {
        var fields = new List<PersonDossierField>
        {
            Field("PersonalNumber", person.PersonalNumber),
            Field("DateOfBirth", FormatDateWithAge(person.DateOfBirth, person.Age)),
            Field("Gender", Describe(person.Gender)),
            Field("Citizenship", Describe(person.Nationality)),
            Field("CountryOfBirth", Describe(person.CountryOfBirth)),
            Field("BirthPlace", person.BirthPlace),
            Field("MaritalStatus", Describe(person.MaritalStatus)),
        };

        if (person.PersonRole == PersonRecordRole.Employee)
        {
            fields.Add(Field("HireDate", FormatDate(person.HireDate)));
            fields.Add(Field("PreviousWorkplacesInTurkmenistan", person.PreviousWorkplacesInTurkmenistan));
            fields.Add(Field("Email", person.Email));
            fields.Add(Field("Subcontractor", Describe(person.Subcontractor)));
        }
        else
        {
            fields.Add(Field("Relationship", Describe(person.Relationship)));
            fields.Add(Field("SponsoringEmployee", person.SponsoringEmployee?.FullName));
        }

        return fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .ToList();
    }

    private static PersonDossierField Field(string labelKey, string? value) => new()
    {
        Label = L($"Field.{labelKey}"),
        Value = value?.Trim() ?? string.Empty,
    };

    // ------------------------------------------------------------ status tiles

    private static IReadOnlyList<PersonDossierStatusTile> BuildStatusTiles(Person person)
    {
        var tiles = new List<PersonDossierStatusTile>();

        var passport = PersonCurrentItems.GetCurrentPassport(person);
        tiles.Add(BuildTile(
            "passport",
            passport?.PassportNumber,
            passport?.ExpirationDate,
            passport?.IsCancelled ?? false,
            passport?.DaysRemaining));

        var visa = PersonCurrentItems.GetCurrentVisa(person);
        tiles.Add(BuildTile(
            "visa",
            visa?.VisaNumber,
            visa?.ExpirationDate,
            visa?.IsCancelled ?? false,
            visa?.DaysRemaining));

        if (person.PersonRole != PersonRecordRole.TemporaryVisitor)
        {
            var workPermit = PersonCurrentItems.GetCurrentWorkPermitItem(person);
            tiles.Add(BuildTile(
                "workPermit",
                workPermit?.WorkPermitNumber,
                workPermit?.ExpirationDate,
                workPermit?.IsCancelled ?? false,
                workPermit?.DaysRemaining));
        }

        var address = PersonCurrentItems.GetCurrentAddressOfResidence(person);
        tiles.Add(BuildTile(
            "registration",
            address == null ? null : Describe(address.City) is { Length: > 0 } city ? city : address.DisplayAddress,
            address?.ExpirationDate,
            false,
            address?.DaysRemaining));

        return tiles;
    }

    private static PersonDossierStatusTile BuildTile(
        string tileId, string? value, DateTime? expiration, bool isCancelled, int? daysRemaining)
    {
        var hasRecord = !string.IsNullOrWhiteSpace(value);
        var (statusLabel, statusCss) = hasRecord
            ? Classify(expiration, isCancelled, daysRemaining)
            : (L("Status.Missing"), string.Empty);

        return new PersonDossierStatusTile
        {
            TileId = tileId,
            Label = L($"Tile.{tileId}"),
            Value = hasRecord ? value!.Trim() : L("Status.None"),
            StatusLabel = statusLabel,
            StatusCssClass = statusCss,
        };
    }

    /// <summary>
    /// Maps an expiry date onto the Report Dashboard status vocabulary so colours stay consistent
    /// across the app (st-approved / st-pending / st-expiring; empty class renders gray).
    /// </summary>
    private static (string Label, string CssClass) Classify(
        DateTime? expiration, bool isCancelled, int? daysRemaining)
    {
        if (isCancelled)
            return (L("Status.Cancelled"), string.Empty);

        if (expiration == null)
            return (string.Empty, string.Empty);

        var days = daysRemaining ?? (int)Math.Floor((expiration.Value.Date - DateTime.Today).TotalDays);

        if (days <= 0)
            return (L("Status.Expired"), "st-expiring");

        if (days <= ExpiringSoonDays)
            return (Format("Status.ExpiresInDays", days), "st-expiring");

        if (days <= ExpiringWatchDays)
            return (Format("Status.ExpiresInDays", days), "st-pending");

        return (Format("Status.ValidTo", FormatDate(expiration)), "st-approved");
    }

    // ---------------------------------------------------------------- sections

    private static IEnumerable<PersonDossierSection> BuildSections(Person person)
    {
        var isEmployee = person.PersonRole == PersonRecordRole.Employee;
        var isVisitor = person.PersonRole == PersonRecordRole.TemporaryVisitor;

        yield return BuildPassports(person);
        yield return BuildVisas(person);

        if (!isVisitor)
            yield return BuildWorkPermits(person);

        if (isEmployee)
        {
            yield return BuildEducation(person);
            yield return BuildPositionHistory(person);
        }

        yield return BuildAddresses(person);
        yield return BuildTravelHistory(person);
        yield return BuildMedicalRecords(person);

        if (isEmployee)
            yield return BuildFamilyMembers(person);

        yield return BuildApplications(person);
        yield return BuildInvitations(person);
        yield return BuildRejections(person);
    }

    private static PersonDossierSection BuildPassports(Person person)
    {
        var current = PersonCurrentItems.GetCurrentPassport(person);

        var records = Safe(person.Passports)
            .OrderByDescending(passport => passport.ExpirationDate ?? DateTime.MinValue)
            .Select(passport =>
            {
                var status = Classify(passport.ExpirationDate, passport.IsCancelled, passport.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"Passport:{passport.ID}",
                    SourceObjectId = passport.ID,
                    SourceObjectType = typeof(Passport),
                    IsCurrent = current != null && current.ID == passport.ID,
                    Cells =
                    [
                        passport.PassportNumber ?? string.Empty,
                        Describe(passport.PassportType),
                        JoinNonEmpty(passport.Authority, Describe(passport.IssuedCountry)),
                        FormatDate(passport.IssueDate),
                        FormatDate(passport.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("passports", 10,
            ["Number", "Type", "IssuedBy", "IssueDate", "Expiry"], records);
    }

    private static PersonDossierSection BuildVisas(Person person)
    {
        var current = PersonCurrentItems.GetCurrentVisa(person);

        // Visas hang off Passport, not Person - flatten across passports.
        var records = Safe(person.Passports)
            .SelectMany(passport => Safe(passport.Visas).Select(visa => (Passport: passport, Visa: visa)))
            .OrderByDescending(pair => pair.Visa.ExpirationDate ?? DateTime.MinValue)
            .Select(pair =>
            {
                var visa = pair.Visa;
                var status = Classify(visa.ExpirationDate, visa.IsCancelled, visa.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"Passport:{pair.Passport.ID}/Visa:{visa.ID}",
                    SourceObjectId = visa.ID,
                    SourceObjectType = typeof(Visa),
                    IsCurrent = current != null && current.ID == visa.ID,
                    Cells =
                    [
                        visa.VisaNumber ?? string.Empty,
                        Describe(visa.VisaCategory),
                        Describe(visa.VisaType),
                        FormatDate(visa.StartDate),
                        FormatDate(visa.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("visas", 20,
            ["Number", "Category", "Type", "ValidFrom", "Expiry"], records);
    }

    private static PersonDossierSection BuildWorkPermits(Person person)
    {
        var current = PersonCurrentItems.GetCurrentWorkPermitItem(person);

        var records = Safe(person.WorkPermitItems)
            .OrderByDescending(item => item.ExpirationDate)
            .Select(item =>
            {
                var status = Classify(item.ExpirationDate, item.IsCancelled, item.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"WorkPermitItem:{item.ID}",
                    SourceObjectId = item.ID,
                    SourceObjectType = typeof(WorkPermitItem),
                    IsCurrent = current != null && current.ID == item.ID,
                    Cells =
                    [
                        item.WorkPermitNumber ?? string.Empty,
                        item.ASNumber ?? string.Empty,
                        FormatDate(item.StartDate),
                        FormatDate(item.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("workPermits", 30,
            ["Number", "ASNumber", "ValidFrom", "Expiry"], records);
    }

    private static PersonDossierSection BuildEducation(Person person)
    {
        var records = Safe(person.Educations)
            .Select(education => new PersonDossierRecord
            {
                RecordKey = $"Education:{education.ID}",
                SourceObjectId = education.ID,
                SourceObjectType = typeof(Education),
                Cells =
                [
                    Describe(education.EducationLevel),
                    Describe(education.EducationInstitution),
                    Describe(education.Specialty),
                    Describe(education.EducationCountry),
                    education.GraduationYear ?? string.Empty,
                ],
            })
            .ToList();

        return Section("education", 40,
            ["Level", "Institution", "Specialty", "Country", "GraduationYear"], records);
    }

    private static PersonDossierSection BuildPositionHistory(Person person)
    {
        var records = Safe(person.PositionHistory)
            .OrderByDescending(history => history.StartDate)
            .Select(history => new PersonDossierRecord
            {
                RecordKey = $"EmployeePositionHistory:{history.ID}",
                SourceObjectId = history.ID,
                SourceObjectType = typeof(EmployeePositionHistory),
                IsCurrent = history.EndDate == null,
                Cells =
                [
                    Describe(history.Position),
                    Describe(history.Department),
                    FormatDate(history.StartDate),
                    history.EndDate == null ? L("Status.Present") : FormatDate(history.EndDate),
                ],
            })
            .ToList();

        return Section("positionHistory", 50,
            ["Position", "Department", "From", "To"], records);
    }

    private static PersonDossierSection BuildAddresses(Person person)
    {
        var current = PersonCurrentItems.GetCurrentAddressOfResidence(person);

        var records = Safe(person.AddressesOfResidence)
            .Select(address =>
            {
                var status = Classify(address.ExpirationDate, false, address.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"AddressOfResidence:{address.ID}",
                    SourceObjectId = address.ID,
                    SourceObjectType = typeof(AddressOfResidence),
                    IsCurrent = current != null && current.ID == address.ID,
                    Cells =
                    [
                        address.Type == null
                            ? string.Empty
                            : LOr($"ResidenceType.{address.Type}", address.Type.ToString()!),
                        Describe(address.City),
                        address.FullAddress ?? string.Empty,
                        FormatDate(address.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("addresses", 60,
            ["Type", "City", "Address", "Expiry"], records);
    }

    private static PersonDossierSection BuildTravelHistory(Person person)
    {
        var records = Safe(person.TravelHistories)
            .OrderByDescending(travel => travel.TravelDate)
            .Select(travel => new PersonDossierRecord
            {
                RecordKey = $"TravelHistory:{travel.ID}",
                SourceObjectId = travel.ID,
                SourceObjectType = typeof(TravelHistory),
                Cells =
                [
                    FormatDate(travel.TravelDate),
                    travel.MovementType == null
                        ? string.Empty
                        : LOr($"MovementType.{travel.MovementType}", travel.MovementType.ToString()!),
                    travel.TravelType == null
                        ? string.Empty
                        : LOr($"TravelType.{travel.TravelType}", travel.TravelType.ToString()!),
                    JoinNonEmpty(
                        Describe(travel.CheckPoint),
                        Describe(travel.Country),
                        Describe(travel.City)),
                ],
            })
            .ToList();

        return Section("travel", 70,
            ["Date", "Movement", "Type", "Place"], records);
    }

    private static PersonDossierSection BuildMedicalRecords(Person person)
    {
        var records = Safe(person.MedicalRecords)
            .OrderByDescending(record => record.IssueDate)
            .Select(record =>
            {
                var status = Classify(record.ExpirationDate, false, record.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"MedicalRecord:{record.ID}",
                    SourceObjectId = record.ID,
                    SourceObjectType = typeof(MedicalRecord),
                    Cells =
                    [
                        record.DocumentNumber ?? string.Empty,
                        FormatDate(record.IssueDate),
                        FormatDate(record.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("medical", 80,
            ["Number", "IssueDate", "Expiry"], records);
    }

    private static PersonDossierSection BuildFamilyMembers(Person person)
    {
        var records = Safe(person.FamilyMembers)
            .Select(member => new PersonDossierRecord
            {
                RecordKey = $"FamilyMember:{member.ID}",
                SourceObjectId = member.ID,
                SourceObjectType = typeof(Person),
                Cells =
                [
                    member.FullName ?? string.Empty,
                    Describe(member.Relationship),
                    FormatDate(member.DateOfBirth),
                    Describe(member.Nationality),
                ],
            })
            .ToList();

        return Section("familyMembers", 90,
            ["Name", "Relationship", "DateOfBirth", "Citizenship"], records);
    }

    private static PersonDossierSection BuildApplications(Person person)
    {
        var apps = Safe(person.ApplicationProfileInstances)
            .OrderByDescending(app => app.ApplicationDate)
            .Select(app => new PersonDossierRecord
            {
                RecordKey = $"Application:{app.ID}",
                SourceObjectId = app.ID,
                SourceObjectType = typeof(ApplicationProfileInstance),
                Cells =
                [
                    app.FullApplicationNumber ?? app.ApplicationNumber ?? string.Empty,
                    app.ApplicationProfile?.Name ?? Describe(app.ApplicationType),
                    FormatDate(app.ApplicationDate),
                ],
                StatusLabel = app.LatestProgress?.State?.NameTm ?? string.Empty,
            })
            .ToList();

        return Section("applications", 100,
            ["ApplicationNumber", "ApplicationProfile", "ApplicationDate"], apps);
    }

    private static PersonDossierSection BuildInvitations(Person person)
    {
        var records = Safe(person.InvitationItems)
            .OrderByDescending(item => item.Invitation?.IssuedDate ?? DateTime.MinValue)
            .Select(item => new PersonDossierRecord
            {
                RecordKey = $"InvitationItem:{item.ID}",
                SourceObjectId = item.ID,
                SourceObjectType = typeof(InvitationItem),
                Cells =
                [
                    item.Invitation?.InvitationNumber ?? string.Empty,
                    Describe(item.Invitation?.VisaCategory),
                    FormatDate(item.Invitation?.IssuedDate),
                    FormatDate(item.Invitation?.ExpirationDate),
                ],
                StatusLabel = item.IsCancelled
                    ? L("Status.Cancelled")
                    : item.IsUsed ? L("Status.Used") : string.Empty,
                StatusCssClass = item.IsCancelled
                    ? string.Empty
                    : item.IsUsed ? "st-approved" : string.Empty,
            })
            .ToList();

        return Section("invitations", 110,
            ["Number", "Category", "Issued", "Expiry"], records);
    }

    private static PersonDossierSection BuildRejections(Person person)
    {
        var records = Safe(person.RejectionItems)
            .Select(item => new PersonDossierRecord
            {
                RecordKey = $"RejectionItem:{item.ID}",
                SourceObjectId = item.ID,
                SourceObjectType = typeof(RejectionItem),
                Cells =
                [
                    item.Rejection?.RejectedDocNumber ?? string.Empty,
                    FormatDate(item.Rejection?.Date),
                    JoinNonEmpty(item.Reason, item.Rejection?.Reason),
                ],
                StatusCssClass = "st-expiring",
            })
            .ToList();

        return Section("rejections", 120,
            ["Document", "Date", "Reason"], records);
    }

    // ----------------------------------------------------------------- helpers

    private static PersonDossierSection Section(
        string sectionId, int sortOrder, string[] columnKeys, List<PersonDossierRecord> records) => new()
        {
            SectionId = sectionId,
            SectionLabel = L($"Section.{sectionId}"),
            SortOrder = sortOrder,
            ColumnHeaders = columnKeys.Select(key => L($"Column.{key}")).ToList(),
            Records = records,
        };

    private static IEnumerable<T> Safe<T>(IList<T>? source) where T : class =>
        source == null ? Enumerable.Empty<T>() : source.Where(item => item != null);

    private static Guid? ResolveId(IObjectSpace objectSpace, Person person)
    {
        var key = objectSpace.GetKeyValue(person);
        return key switch
        {
            Guid guid => guid,
            null => null,
            _ => Guid.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : null,
        };
    }

    private static string? BuildPhotoDataUri(byte[]? photo) =>
        photo is { Length: > 0 }
            ? $"data:image/png;base64,{Convert.ToBase64String(photo)}"
            : null;

    /// <summary>Lookup catalogs render via <c>LocalizedDisplayName</c>; everything else via ToString.</summary>
    private static string Describe(object? value) => value switch
    {
        null => string.Empty,
        LookupBase lookup => lookup.LocalizedDisplayName ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static string JoinNonEmpty(params string?[] parts) =>
        string.Join(" - ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    private static string FormatDate(DateTime? value) =>
        value == null || value.Value == default
            ? string.Empty
            : value.Value.ToString(CurrentDateFormat, CultureInfo.CurrentUICulture);

    private static string FormatDateWithAge(DateTime value, int age)
    {
        var formatted = FormatDate(value);
        return string.IsNullOrEmpty(formatted) ? string.Empty : $"{formatted} ({age})";
    }

    private static string L(string suffix) =>
        VisaUiMessages.Get($"PersonDossier.{suffix}");

    /// <summary>
    /// Localized text for enum-backed labels, falling back to the enum name so an unseeded key
    /// never leaks a raw resource id into the UI.
    /// </summary>
    private static string LOr(string suffix, string fallback)
    {
        var key = $"PersonDossier.{suffix}";
        var value = VisaUiMessages.Get(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static string Format(string suffix, params object[] args) =>
        VisaUiMessages.Format($"PersonDossier.{suffix}", args);
}

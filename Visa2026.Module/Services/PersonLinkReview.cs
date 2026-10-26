using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

public sealed class PersonLinkReviewLine
{
    public ApplicationProfileInstancePersonLinkKind Kind { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Summary { get; set; } = "—";
    public Guid ObjectId { get; set; }
    public IReadOnlyList<PersonLinkReviewChoice> Choices { get; set; } = Array.Empty<PersonLinkReviewChoice>();
    public IReadOnlyList<PersonLinkReviewField> Fields { get; set; } = Array.Empty<PersonLinkReviewField>();
}

public sealed class PersonLinkReviewChoice
{
    public Guid ObjectId { get; init; }
    public string Label { get; init; } = "—";
    public IReadOnlyList<PersonLinkReviewField> Fields { get; init; } = Array.Empty<PersonLinkReviewField>();
}

public sealed class PersonLinkReviewField
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public bool Required { get; init; }
    public bool Lookup { get; init; }
}

public sealed class PersonLinkReviewChoiceRequest
{
    public PersonLinkReviewChoiceRequest(ApplicationProfileInstancePersonLinkKind kind, Guid objectId)
    {
        Kind = kind;
        ObjectId = objectId;
    }

    public ApplicationProfileInstancePersonLinkKind Kind { get; }
    public Guid ObjectId { get; }
}

public sealed class PersonLinkReviewCreateOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? ParentId { get; set; }
}

public sealed class PersonLinkReviewCreateField
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Editor { get; set; } = "text";
    public string? ShowWhenKey { get; set; }
    public string? ShowWhenValue { get; set; }
    public string? FilterByKey { get; set; }
    public List<PersonLinkReviewCreateOption> Options { get; set; } = new();
}

public sealed class PersonLinkReviewCreateForm
{
    public ApplicationProfileInstancePersonLinkKind Kind { get; set; }
    public Guid ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Error { get; set; }
    public bool Saved { get; set; }
    public List<PersonLinkReviewCreateField> Fields { get; set; } = new();
}

/// <summary>
/// Overview shown before Join commits a person onto an Application Profile instance.
/// </summary>
public static class PersonLinkReview
{
    public static IReadOnlyList<PersonLinkReviewLine> Build(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null)
            return Array.Empty<PersonLinkReviewLine>();

        ApplicationProfileInstancePersonResolver.EnsureApplicationProfileLoaded(objectSpace, application);
        var lines = new List<PersonLinkReviewLine>();
        foreach (var definition in ApplicationWorkspace.ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
        {
            if (!definition.IsConfigured(application))
                continue;

            lines.Add(definition.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                ? AddressLine(objectSpace, application, person, definition.Label)
                : Line(objectSpace, definition.Kind, definition.Label, person));
        }

        return lines;
    }

    /// <summary>
    /// Address the officer can edit from the pen. Uses the newest address, or creates one
    /// from the Case summary stay when the person has none. The caller commits.
    /// </summary>
    public static Guid EnsureAddressToEdit(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || person == null || person.ID == Guid.Empty)
            return Guid.Empty;

        var last = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(address => address.Person != null && address.Person.ID == person.ID)
            .OrderByDescending(address => address.ID)
            .FirstOrDefault();
        if (last != null)
            return last.ID;

        var stay = application?.InvitationAddress;
        if (!InvitationStayAddressCopy.IsComplete(stay))
            return Guid.Empty;

        return InvitationStayAddressCopy.CreateFromStay(objectSpace, person, stay!)?.ID ?? Guid.Empty;
    }

    public static void ApplyChoice(PersonLinkReviewLine line, Guid objectId)
    {
        if (line == null)
            return;

        var choice = line.Choices.FirstOrDefault(item => item.ObjectId == objectId);
        if (choice == null)
            return;

        line.ObjectId = choice.ObjectId;
        line.Summary = string.IsNullOrWhiteSpace(choice.Label) ? "—" : choice.Label;
        line.Fields = choice.Fields;
    }

    /// <summary>
    /// After the officer saves a new record from the plus button, select it for Join.
    /// </summary>
    public static void SelectSaved(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Person person,
        PersonLinkReviewLine line,
        Guid savedId)
    {
        if (objectSpace == null || person == null || line == null || savedId == Guid.Empty)
            return;

        if (application != null)
            ApplicationProfileInstancePersonResolver.EnsureApplicationProfileLoaded(objectSpace, application);

        var choices = line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
            ? AddressChoicesFor(objectSpace, application, person)
            : ChoicesFor(objectSpace, line.Kind, person.ID).ToList();

        if (choices.All(choice => choice.ObjectId != savedId)
            && ChoiceForObject(objectSpace, line.Kind, line.Label, savedId) is { } extra)
            choices.Add(extra);

        line.Choices = choices;
        ApplyChoice(line, savedId);
    }

    /// <summary>
    /// After <c>LinkPerson</c>, point this case at the record the officer picked in the review.
    /// </summary>
    public static void LinkSelected(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid objectId)
    {
        if (objectSpace == null || application == null || person == null || objectId == Guid.Empty)
            return;

        var type = ObjectType(kind);
        if (type == null || objectSpace.GetObjectByKey(type, objectId) == null)
            return;

        var link = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(row => row.ApplicationProfileInstanceId == application.ID
                && row.PersonId == person.ID
                && row.LinkKind == kind
                && row.LinkedObjectId != null
                && row.LinkedObjectId != Guid.Empty)
            .FirstOrDefault();

        if (link?.LinkedObjectId is not Guid previous || previous == Guid.Empty || previous == objectId)
        {
            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace, application, person, kind, objectId);
            return;
        }

        ApplicationProfileInstanceChildMembership.Remove(application, kind, previous, objectSpace);
        link.LinkedObjectId = objectId;
        ApplicationProfileInstanceChildMembership.Add(objectSpace, application, kind, objectId);
    }

    public static void ApplyJoinLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine> lines)
    {
        foreach (var line in lines ?? Array.Empty<PersonLinkReviewLine>())
        {
            if (line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence && line.ObjectId == Guid.Empty)
            {
                InvitationStayAddressCopy.AcceptOnJoin(objectSpace, application, person);
                continue;
            }

            if (line.ObjectId != Guid.Empty)
                LinkSelected(objectSpace, application, person, line.Kind, line.ObjectId);
        }
    }

    public static string DescribeAddress(IObjectSpace objectSpace, Guid addressId)
    {
        if (objectSpace == null || addressId == Guid.Empty)
            return "—";

        return AddressSummary(objectSpace.GetObjectByKey<AddressOfResidence>(addressId));
    }

    public static Type? ObjectType(ApplicationProfileInstancePersonLinkKind kind) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Passport => typeof(Passport),
        ApplicationProfileInstancePersonLinkKind.Education => typeof(Education),
        ApplicationProfileInstancePersonLinkKind.Position => typeof(EmployeePositionHistory),
        ApplicationProfileInstancePersonLinkKind.WorkDuty => typeof(WorkDuty),
        ApplicationProfileInstancePersonLinkKind.AddressOfResidence => typeof(AddressOfResidence),
        ApplicationProfileInstancePersonLinkKind.Visa => typeof(Visa),
        ApplicationProfileInstancePersonLinkKind.InvitationItem => typeof(InvitationItem),
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => typeof(WorkPermitItem),
        ApplicationProfileInstancePersonLinkKind.BorderZoneItem => typeof(BorderZoneItem),
        ApplicationProfileInstancePersonLinkKind.Salary => typeof(EmployeeSalary),
        ApplicationProfileInstancePersonLinkKind.MedicalRecord => typeof(MedicalRecord),
        ApplicationProfileInstancePersonLinkKind.RejectionItem => typeof(RejectionItem),
        ApplicationProfileInstancePersonLinkKind.TravelHistory => typeof(TravelHistory),
        _ => null,
    };

    private static PersonLinkReviewLine AddressLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string label)
    {
        var stay = application.InvitationAddress;
        if (InvitationStayAddressCopy.IsComplete(stay)
            && ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(application.ApplicationProfile))
        {
            var last = objectSpace.GetObjectsQuery<AddressOfResidence>()
                .Where(address => address.Person != null && address.Person.ID == person.ID)
                .OrderByDescending(address => address.ID)
                .FirstOrDefault();
            var matches = InvitationStayAddressCopy.Matches(last, stay);
            var stayLine = new PersonLinkReviewLine
            {
                Kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                Label = "Address of residence",
                Summary = StaySummary(stay!),
                ObjectId = matches ? last!.ID : Guid.Empty,
            };
            Attach(stayLine, AddressChoices(objectSpace, person.ID, stay, includeStayChoice: !matches));
            return stayLine;
        }

        var current = ApplicationProfileInstancePersonValidItems.ResolveAddress(person);
        var currentLine = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            Label = "Address of residence",
            Summary = AddressSummary(current),
            ObjectId = current?.ID ?? Guid.Empty,
        };
        Attach(currentLine, AddressChoices(objectSpace, person.ID, stay: null, includeStayChoice: false));
        return currentLine;
    }

    private static PersonLinkReviewLine Line(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        string label,
        Person person)
    {
        var (summary, id) = kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport => PassportSummary(ApplicationProfileInstancePersonValidItems.ResolvePassport(person)),
            ApplicationProfileInstancePersonLinkKind.Education => EducationSummary(ApplicationProfileInstancePersonValidItems.ResolveEducation(person)),
            ApplicationProfileInstancePersonLinkKind.Position => PositionSummary(ApplicationProfileInstancePersonValidItems.ResolvePosition(person)),
            ApplicationProfileInstancePersonLinkKind.WorkDuty => TextId(ApplicationProfileInstancePersonValidItems.ResolveWorkDuty(person)?.Description, ApplicationProfileInstancePersonValidItems.ResolveWorkDuty(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.Visa => VisaSummary(ApplicationProfileInstancePersonValidItems.ResolveVisa(person)),
            ApplicationProfileInstancePersonLinkKind.InvitationItem => TextId(ApplicationProfileInstancePersonValidItems.ResolveInvitationItem(person)?.InvitationItemName, ApplicationProfileInstancePersonValidItems.ResolveInvitationItem(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.WorkPermitItem => TextId(ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person)?.WorkPermitNumber, ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.BorderZoneItem => TextId(ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItem(objectSpace, person)?.Passport?.PassportNumber, ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItem(objectSpace, person)?.ID),
            ApplicationProfileInstancePersonLinkKind.Salary => TextId(ApplicationProfileInstancePersonValidItems.ResolveSalary(person)?.Amount, ApplicationProfileInstancePersonValidItems.ResolveSalary(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.MedicalRecord => TextId(DateText(ApplicationProfileInstancePersonValidItems.ResolveMedical(person)?.IssueDate), ApplicationProfileInstancePersonValidItems.ResolveMedical(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.RejectionItem => TextId(ApplicationProfileInstancePersonValidItems.ResolveRejectionItem(person)?.RejectionItemName, ApplicationProfileInstancePersonValidItems.ResolveRejectionItem(person)?.ID),
            ApplicationProfileInstancePersonLinkKind.TravelHistory => TextId(ApplicationProfileInstancePersonValidItems.ResolveTravelHistory(person)?.Title, ApplicationProfileInstancePersonValidItems.ResolveTravelHistory(person)?.ID),
            _ => ( "—", Guid.Empty ),
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

    private static void Attach(PersonLinkReviewLine line, IReadOnlyList<PersonLinkReviewChoice> choices)
    {
        if (choices.Count == 0)
        {
            choices = new[]
            {
                new PersonLinkReviewChoice
                {
                    ObjectId = line.ObjectId,
                    Label = line.Summary,
                    Fields = TextFields(line.Label, line.Summary),
                },
            };
        }

        line.Choices = choices;
        line.Fields = choices.FirstOrDefault(choice => choice.ObjectId == line.ObjectId)?.Fields
            ?? choices[0].Fields;
    }

    private static IReadOnlyList<PersonLinkReviewChoice> ChoicesFor(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        Guid personId) => kind switch
    {
        ApplicationProfileInstancePersonLinkKind.Passport => objectSpace.GetObjectsQuery<Passport>()
            .Where(row => row.Person != null && row.Person.ID == personId)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Select(row => Choice(row.ID, PassportSummary(row).Summary, PassportFields(row)))
            .ToList(),
        ApplicationProfileInstancePersonLinkKind.Education => objectSpace.GetObjectsQuery<Education>()
            .Where(row => row.Person != null && row.Person.ID == personId)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Select(row => Choice(row.ID, EducationSummary(row).Summary, EducationFields(row)))
            .ToList(),
        ApplicationProfileInstancePersonLinkKind.Position => objectSpace.GetObjectsQuery<EmployeePositionHistory>()
            .Where(row => row.Person != null && row.Person.ID == personId)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Select(row => Choice(row.ID, PositionSummary(row).Summary, PositionFields(row)))
            .ToList(),
        ApplicationProfileInstancePersonLinkKind.Visa => objectSpace.GetObjectsQuery<Visa>()
            .Where(row => row.Passport != null && row.Passport.Person != null && row.Passport.Person.ID == personId)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Select(row => Choice(row.ID, VisaSummary(row).Summary, VisaFields(row)))
            .ToList(),
        _ => Array.Empty<PersonLinkReviewChoice>(),
    };

    private static List<PersonLinkReviewChoice> AddressChoicesFor(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Person person)
    {
        var stay = application?.InvitationAddress;
        var includeStay = application?.ApplicationProfile != null
            && InvitationStayAddressCopy.IsComplete(stay)
            && ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(application.ApplicationProfile);
        if (includeStay)
        {
            var last = objectSpace.GetObjectsQuery<AddressOfResidence>()
                .Where(address => address.Person != null && address.Person.ID == person.ID)
                .OrderByDescending(address => address.ID)
                .FirstOrDefault();
            includeStay = !InvitationStayAddressCopy.Matches(last, stay);
        }

        return AddressChoices(objectSpace, person.ID, stay, includeStay).ToList();
    }

    private static PersonLinkReviewChoice? ChoiceForObject(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        string label,
        Guid objectId)
    {
        switch (kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                var passport = objectSpace.GetObjectByKey<Passport>(objectId);
                return passport == null ? null : Choice(passport.ID, PassportSummary(passport).Summary, PassportFields(passport));
            case ApplicationProfileInstancePersonLinkKind.Education:
                var education = objectSpace.GetObjectByKey<Education>(objectId);
                return education == null ? null : Choice(education.ID, EducationSummary(education).Summary, EducationFields(education));
            case ApplicationProfileInstancePersonLinkKind.Position:
                var position = objectSpace.GetObjectByKey<EmployeePositionHistory>(objectId);
                return position == null ? null : Choice(position.ID, PositionSummary(position).Summary, PositionFields(position));
            case ApplicationProfileInstancePersonLinkKind.Visa:
                var visa = objectSpace.GetObjectByKey<Visa>(objectId);
                return visa == null ? null : Choice(visa.ID, VisaSummary(visa).Summary, VisaFields(visa));
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                var address = objectSpace.GetObjectByKey<AddressOfResidence>(objectId);
                return address == null ? null : Choice(address.ID, AddressSummary(address), AddressFields(address));
            default:
                var type = ObjectType(kind);
                var target = type == null ? null : objectSpace.GetObjectByKey(type, objectId);
                if (target == null)
                    return null;
                var text = target.ToString();
                return Choice(objectId, text, TextFields(label, string.IsNullOrWhiteSpace(text) ? string.Empty : text));
        }
    }

    private static IReadOnlyList<PersonLinkReviewChoice> AddressChoices(
        IObjectSpace objectSpace,
        Guid personId,
        InvitationAddress? stay,
        bool includeStayChoice)
    {
        var choices = new List<PersonLinkReviewChoice>();
        if (includeStayChoice && stay != null)
            choices.Add(Choice(Guid.Empty, StaySummary(stay), StayFields(stay)));

        choices.AddRange(objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(row => row.Person != null && row.Person.ID == personId)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Select(row => Choice(row.ID, AddressSummary(row), AddressFields(row))));
        return choices;
    }

    private static PersonLinkReviewChoice Choice(Guid objectId, string? label, IReadOnlyList<PersonLinkReviewField> fields) =>
        new()
        {
            ObjectId = objectId,
            Label = string.IsNullOrWhiteSpace(label) ? "—" : label,
            Fields = fields,
        };

    private static IReadOnlyList<PersonLinkReviewField> TextFields(string label, string value) =>
        new[] { Field(label, value == "—" ? string.Empty : value, required: false, lookup: false) };

    private static PersonLinkReviewField Field(string label, string? value, bool required, bool lookup) =>
        new()
        {
            Label = label,
            Value = value?.Trim() ?? string.Empty,
            Required = required,
            Lookup = lookup,
        };

    private static IReadOnlyList<PersonLinkReviewField> PassportFields(Passport passport) => new[]
    {
        Field("Passport number", passport.PassportNumber, true, false),
        Field("Passport type", passport.PassportType?.NameTm, true, true),
        Field("Issued country", passport.IssuedCountry?.NameTm, true, true),
        Field("Authority", passport.Authority, true, false),
        Field("Issue date", DateText(passport.IssueDate), true, false),
        Field("Expiration date", DateText(passport.ExpirationDate), true, false),
    };

    private static IReadOnlyList<PersonLinkReviewField> EducationFields(Education education) => new[]
    {
        Field("Education level", education.EducationLevel?.NameTm, true, true),
        Field("Institution", education.EducationInstitution?.NameTm, true, true),
        Field("Country", education.EducationCountry?.NameTm, true, true),
        Field("Specialty", education.Specialty?.NameTm, true, true),
    };

    private static IReadOnlyList<PersonLinkReviewField> PositionFields(EmployeePositionHistory position) => new[]
    {
        Field("Position (visa reports)", position.Position?.NameTm, true, true),
        Field("Position (actual / company)", position.ActualPosition?.Name, true, true),
        Field("Department", position.Department?.NameTm, false, true),
    };

    private static IReadOnlyList<PersonLinkReviewField> VisaFields(Visa visa) => new[]
    {
        Field("Visa number", visa.VisaNumber, true, false),
        Field("Visa type", visa.VisaType?.NameTm, true, true),
        Field("Visa category", visa.VisaCategory?.NameTm, true, true),
        Field("Issued place", visa.VisaIssuedPlace?.NameTm, true, true),
        Field("Issue date", DateText(visa.IssueDate), true, false),
        Field("Start date", DateText(visa.StartDate), true, false),
        Field("Expiration date", DateText(visa.ExpirationDate), true, false),
        Field("Days remaining", visa.DaysRemaining.ToString(CultureInfo.InvariantCulture), false, false),
        Field("Border zone", visa.BorderZoneLocation, true, false),
    };

    private static IReadOnlyList<PersonLinkReviewField> StayFields(InvitationAddress stay) => new[]
    {
        Field("Region", stay.Region?.NameTm, true, true),
        Field("City", stay.City?.NameTm, true, true),
        Field("Type", TypeLabel(stay.Type), true, true),
        Field(PlaceLabel(stay.Type), Place(stay), true, stay.Type != ResidenceType.PrivateHouse),
    };

    private static IReadOnlyList<PersonLinkReviewField> AddressFields(AddressOfResidence address) => new[]
    {
        Field("Region", address.Region?.NameTm, true, true),
        Field("City", address.City?.NameTm, true, true),
        Field("Type", TypeLabel(address.Type), true, true),
        Field(PlaceLabel(address.Type), AddressPlace(address), true, address.Type != ResidenceType.PrivateHouse),
    };

    private static string? AddressPlace(AddressOfResidence address) => address.Type switch
    {
        ResidenceType.Lodging => address.Lodging?.FullAddress ?? address.FullAddress,
        ResidenceType.Hotel => address.Hotel?.Name,
        ResidenceType.Hospital => address.Hospital?.Name,
        ResidenceType.Other => address.OtherSite?.FullAddress ?? address.FullAddress,
        ResidenceType.PrivateHouse => address.FullAddress,
        _ => address.FullAddress,
    };

    private static string PlaceLabel(ResidenceType? type) => type switch
    {
        ResidenceType.Hotel => "Hotel",
        ResidenceType.Hospital => "Hospital",
        ResidenceType.Other => "Other",
        ResidenceType.PrivateHouse => "Private house",
        _ => "Lodging",
    };

    private static (string Summary, Guid? Id) PassportSummary(Passport? passport) =>
        (JoinParts(passport?.PassportNumber, passport?.PassportType?.NameTm, passport?.IssuedCountry?.NameTm, DateText(passport?.ExpirationDate)), passport?.ID);

    private static (string Summary, Guid? Id) EducationSummary(Education? education) =>
        (JoinParts(education?.EducationLevel?.NameTm, education?.EducationInstitution?.NameTm, education?.Specialty?.NameTm), education?.ID);

    private static (string Summary, Guid? Id) PositionSummary(EmployeePositionHistory? position) =>
        (JoinParts(position?.Position?.NameTm, position?.Department?.NameTm, DateText(position?.StartDate)), position?.ID);

    private static (string Summary, Guid? Id) VisaSummary(Visa? visa) =>
        (JoinParts(visa?.VisaNumber, visa?.VisaType?.NameTm, DateText(visa?.ExpirationDate)), visa?.ID);

    private static (string Summary, Guid? Id) TextId(string? text, Guid? id) => (text ?? string.Empty, id);

    private static string StaySummary(InvitationAddress stay) =>
        JoinParts(stay.Region?.NameTm, stay.City?.NameTm, TypeLabel(stay.Type), Place(stay));

    private static string AddressSummary(AddressOfResidence? address) =>
        address == null
            ? "—"
            : JoinParts(address.Region?.NameTm, address.City?.NameTm, TypeLabel(address.Type), address.FullAddress);

    private static string Place(InvitationAddress stay) => stay.Type switch
    {
        ResidenceType.Lodging => stay.Lodging?.FullAddress,
        ResidenceType.Hotel => stay.Hotel?.Name,
        ResidenceType.Hospital => stay.Hospital?.Name,
        ResidenceType.Other => stay.OtherSite?.FullAddress,
        ResidenceType.PrivateHouse => stay.PrivateHouseAddress,
        _ => null,
    };

    private static string? TypeLabel(ResidenceType? type) => type switch
    {
        ResidenceType.Lodging => "Lodging",
        ResidenceType.Hotel => "Hotel",
        ResidenceType.Hospital => "Hospital",
        ResidenceType.Other => "Other",
        ResidenceType.PrivateHouse => "Private house",
        _ => null,
    };

    private static string JoinParts(params string?[] parts)
    {
        var values = parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()).ToList();
        return values.Count == 0 ? "—" : string.Join(", ", values);
    }

    private static string? DateText(DateTime? date) =>
        date.HasValue && date.Value != default
            ? date.Value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : null;

    private static string? DateText(DateTime date) =>
        date == default ? null : date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    public static PersonLinkReviewCreateForm BuildCreateForm(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        string title)
    {
        var form = new PersonLinkReviewCreateForm { Kind = kind, Title = title };
        form.Fields = kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport => PassportCreateFields(objectSpace),
            ApplicationProfileInstancePersonLinkKind.Education => EducationCreateFields(objectSpace),
            ApplicationProfileInstancePersonLinkKind.Position => PositionCreateFields(objectSpace),
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence => AddressCreateFields(objectSpace),
            ApplicationProfileInstancePersonLinkKind.Visa => VisaCreateFields(objectSpace),
            _ => new List<PersonLinkReviewCreateField>(),
        };
        if (form.Fields.Count == 0)
            form.Error = "This record cannot be added from this form.";
        return form;
    }

    /// <summary>
    /// Same form as <see cref="BuildCreateForm"/>, filled from the record the pen opened.
    /// An address line that still shows the Case summary stay stays unsaved until Save.
    /// </summary>
    public static PersonLinkReviewCreateForm BuildEditForm(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? application,
        Person? person,
        PersonLinkReviewLine line)
    {
        var form = BuildCreateForm(objectSpace, line.Kind, line.Label);
        if (form.Fields.Count == 0)
            return form;

        if (line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence && line.ObjectId == Guid.Empty)
        {
            FillAddress(form, application?.InvitationAddress);
            return form;
        }

        if (line.ObjectId == Guid.Empty || !FillExisting(objectSpace, person, form, line))
        {
            form.ObjectId = Guid.Empty;
            form.Fields.Clear();
            form.Error = "This record was not found.";
            return form;
        }

        form.ObjectId = line.ObjectId;
        return form;
    }

    public static Guid TryCreate(
        IObjectSpace objectSpace,
        Person person,
        PersonLinkReviewCreateForm form,
        Guid passportId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || person == null || form == null)
        {
            error = "Could not save the record.";
            return Guid.Empty;
        }

        var missing = form.Fields.FirstOrDefault(field => field.Required && IsShown(form, field) && string.IsNullOrWhiteSpace(field.Value));
        if (missing != null)
        {
            error = missing.Label + " is required.";
            return Guid.Empty;
        }

        BaseObject? created = form.Kind switch
        {
            ApplicationProfileInstancePersonLinkKind.Passport => CreatePassport(objectSpace, person, form, out error),
            ApplicationProfileInstancePersonLinkKind.Education => CreateEducation(objectSpace, person, form, out error),
            ApplicationProfileInstancePersonLinkKind.Position => CreatePosition(objectSpace, person, form, out error),
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence => CreateAddress(objectSpace, person, form, out error),
            ApplicationProfileInstancePersonLinkKind.Visa => CreateVisa(objectSpace, person, form, passportId, out error),
            _ => null,
        };
        if (created == null && error == null)
            error = form.ObjectId == Guid.Empty
                ? "This record cannot be added from this form."
                : "This record cannot be edited from this form.";
        return created?.ID ?? Guid.Empty;
    }

    private static bool FillExisting(
        IObjectSpace objectSpace,
        Person? person,
        PersonLinkReviewCreateForm form,
        PersonLinkReviewLine line)
    {
        switch (line.Kind)
        {
            case ApplicationProfileInstancePersonLinkKind.Passport:
                var passport = objectSpace.GetObjectByKey<Passport>(line.ObjectId);
                if (passport == null || passport.Person?.ID != person?.ID)
                    return false;
                Set(form, "number", passport.PassportNumber);
                Set(form, "type", IdText(passport.PassportType));
                Set(form, "country", IdText(passport.IssuedCountry));
                Set(form, "authority", passport.Authority);
                Set(form, "issue", DateText(passport.IssueDate));
                Set(form, "expiration", DateText(passport.ExpirationDate));
                return true;
            case ApplicationProfileInstancePersonLinkKind.Education:
                var education = objectSpace.GetObjectByKey<Education>(line.ObjectId);
                if (education == null || education.Person?.ID != person?.ID)
                    return false;
                Set(form, "level", IdText(education.EducationLevel));
                Set(form, "institution", IdText(education.EducationInstitution));
                Set(form, "country", IdText(education.EducationCountry));
                Set(form, "specialty", IdText(education.Specialty));
                return true;
            case ApplicationProfileInstancePersonLinkKind.Position:
                var position = objectSpace.GetObjectByKey<EmployeePositionHistory>(line.ObjectId);
                if (position == null || position.Person?.ID != person?.ID)
                    return false;
                Set(form, "position", IdText(position.Position));
                Set(form, "actual", IdText(position.ActualPosition));
                Set(form, "department", IdText(position.Department));
                return true;
            case ApplicationProfileInstancePersonLinkKind.AddressOfResidence:
                var address = objectSpace.GetObjectByKey<AddressOfResidence>(line.ObjectId);
                if (address == null || address.Person?.ID != person?.ID)
                    return false;
                FillAddress(form, address);
                return true;
            case ApplicationProfileInstancePersonLinkKind.Visa:
                var visa = objectSpace.GetObjectByKey<Visa>(line.ObjectId);
                if (visa == null || visa.Passport?.Person?.ID != person?.ID)
                    return false;
                Set(form, "number", visa.VisaNumber);
                Set(form, "type", IdText(visa.VisaType));
                Set(form, "category", IdText(visa.VisaCategory));
                Set(form, "place", IdText(visa.VisaIssuedPlace));
                Set(form, "issue", DateText(visa.IssueDate));
                Set(form, "start", DateText(visa.StartDate));
                Set(form, "expiration", DateText(visa.ExpirationDate));
                Set(form, "border", BorderId(objectSpace, visa.BorderZoneLocation));
                return true;
            default:
                return false;
        }
    }

    private static void FillAddress(PersonLinkReviewCreateForm form, InvitationAddress? stay)
    {
        if (stay == null)
            return;

        Set(form, "region", IdText(stay.Region));
        Set(form, "city", IdText(stay.City));
        if (stay.Type is { } type)
            Set(form, "type", type.ToString());
        Set(form, "lodging", IdText(stay.Lodging));
        Set(form, "hotel", IdText(stay.Hotel));
        Set(form, "hospital", IdText(stay.Hospital));
        Set(form, "other", IdText(stay.OtherSite));
        Set(form, "private", stay.PrivateHouseAddress);
    }

    private static void FillAddress(PersonLinkReviewCreateForm form, AddressOfResidence address)
    {
        Set(form, "region", IdText(address.Region));
        Set(form, "city", IdText(address.City));
        if (address.Type is { } type)
            Set(form, "type", type.ToString());
        Set(form, "lodging", IdText(address.Lodging));
        Set(form, "hotel", IdText(address.Hotel));
        Set(form, "hospital", IdText(address.Hospital));
        Set(form, "other", IdText(address.OtherSite));
        if (address.Type == ResidenceType.PrivateHouse)
            Set(form, "private", address.FullAddress);
    }

    private static void Set(PersonLinkReviewCreateForm form, string key, string? value)
    {
        var field = form.Fields.FirstOrDefault(item => item.Key == key);
        if (field != null)
            field.Value = value ?? string.Empty;
    }

    private static string IdText(BaseObject? row) =>
        row == null || row.ID == Guid.Empty ? string.Empty : row.ID.ToString();

    private static string BorderId(IObjectSpace objectSpace, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var match = objectSpace.GetObjectsQuery<BorderZoneName>()
            .AsEnumerable()
            .FirstOrDefault(row => string.Equals(row.NameTm?.Trim(), name.Trim(), StringComparison.Ordinal));
        return match == null ? string.Empty : match.ID.ToString();
    }

    private static bool IsShown(PersonLinkReviewCreateForm form, PersonLinkReviewCreateField field)
    {
        if (string.IsNullOrEmpty(field.ShowWhenKey))
            return true;
        var other = form.Fields.FirstOrDefault(item => item.Key == field.ShowWhenKey);
        return string.Equals(other?.Value, field.ShowWhenValue, StringComparison.Ordinal);
    }

    private static string Value(PersonLinkReviewCreateForm form, string key) =>
        form.Fields.FirstOrDefault(field => field.Key == key)?.Value?.Trim() ?? string.Empty;

    private static T? Lookup<T>(IObjectSpace objectSpace, string? id) where T : class
    {
        if (!Guid.TryParse(id, out var key) || key == Guid.Empty)
            return null;
        return objectSpace.GetObjectByKey<T>(key);
    }

    private static bool TryDate(string? text, out DateTime date) =>
        DateTime.TryParseExact(text?.Trim(), "dd.MM.yyyy", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);

    private static List<PersonLinkReviewCreateOption> CatalogOptions<T>(IObjectSpace objectSpace) where T : LookupBase =>
        objectSpace.GetObjectsQuery<T>()
            .OrderBy(row => row.NameTm)
            .AsEnumerable()
            .Where(row => !string.IsNullOrWhiteSpace(row.NameTm))
            .Select(row => new PersonLinkReviewCreateOption { Id = row.ID.ToString(), Label = row.NameTm })
            .ToList();

    private static PersonLinkReviewCreateField LookupField(string key, string label, bool required, List<PersonLinkReviewCreateOption> options, string? showWhenKey = null, string? showWhenValue = null, string? filterByKey = null) =>
        new()
        {
            Key = key,
            Label = label,
            Required = required,
            Editor = "lookup",
            Options = options,
            ShowWhenKey = showWhenKey,
            ShowWhenValue = showWhenValue,
            FilterByKey = filterByKey,
        };

    private static PersonLinkReviewCreateField TextField(string key, string label, bool required, bool date = false) =>
        new() { Key = key, Label = label, Required = required, Editor = date ? "date" : "text" };

    private static List<PersonLinkReviewCreateField> PassportCreateFields(IObjectSpace objectSpace) => new()
    {
        TextField("number", "Passport number", true),
        LookupField("type", "Passport type", true, CatalogOptions<PassportType>(objectSpace)),
        LookupField("country", "Issued country", true, CatalogOptions<Country>(objectSpace)),
        TextField("authority", "Authority", true),
        TextField("issue", "Issue date", true, date: true),
        TextField("expiration", "Expiration date", true, date: true),
    };

    private static List<PersonLinkReviewCreateField> EducationCreateFields(IObjectSpace objectSpace) => new()
    {
        LookupField("level", "Education level", true, CatalogOptions<EducationLevel>(objectSpace)),
        LookupField("institution", "Institution", true, CatalogOptions<EducationInstitution>(objectSpace)),
        LookupField("country", "Country", true, CatalogOptions<Country>(objectSpace)),
        LookupField("specialty", "Specialty", true, CatalogOptions<Specialty>(objectSpace)),
    };

    private static List<PersonLinkReviewCreateField> PositionCreateFields(IObjectSpace objectSpace) => new()
    {
        LookupField("position", "Position (visa reports)", true, CatalogOptions<Position>(objectSpace)),
        LookupField("actual", "Position (actual / company)", true, ActualPositionOptions(objectSpace)),
        LookupField("department", "Department", false, CatalogOptions<Department>(objectSpace)),
    };

    private static List<PersonLinkReviewCreateOption> ActualPositionOptions(IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<ActualPosition>()
            .OrderBy(row => row.Name)
            .AsEnumerable()
            .Where(row => !string.IsNullOrWhiteSpace(row.Name))
            .Select(row => new PersonLinkReviewCreateOption { Id = row.ID.ToString(), Label = row.Name })
            .ToList();

    private static List<PersonLinkReviewCreateField> VisaCreateFields(IObjectSpace objectSpace) => new()
    {
        TextField("number", "Visa number", true),
        LookupField("type", "Visa type", true, CatalogOptions<VisaType>(objectSpace)),
        LookupField("category", "Visa category", true, CatalogOptions<VisaCategory>(objectSpace)),
        LookupField("place", "Issued place", true, CatalogOptions<VisaIssuedPlace>(objectSpace)),
        TextField("issue", "Issue date", true, date: true),
        TextField("start", "Start date", true, date: true),
        TextField("expiration", "Expiration date", true, date: true),
        LookupField("border", "Border zone", true, CatalogOptions<BorderZoneName>(objectSpace)),
    };

    private static List<PersonLinkReviewCreateField> AddressCreateFields(IObjectSpace objectSpace)
    {
        var cities = objectSpace.GetObjectsQuery<City>()
            .OrderBy(row => row.NameTm)
            .AsEnumerable()
            .Where(row => !string.IsNullOrWhiteSpace(row.NameTm))
            .Select(row => new PersonLinkReviewCreateOption { Id = row.ID.ToString(), Label = row.NameTm, ParentId = row.Region?.ID.ToString() })
            .ToList();
        var types = Enum.GetValues<ResidenceType>()
            .Select(type => new PersonLinkReviewCreateOption { Id = type.ToString(), Label = TypeLabel(type) ?? type.ToString() })
            .ToList();
        var privateHouse = TextField("private", "Private house", true);
        privateHouse.ShowWhenKey = "type";
        privateHouse.ShowWhenValue = ResidenceType.PrivateHouse.ToString();
        return new List<PersonLinkReviewCreateField>
        {
            LookupField("region", "Region", true, CatalogOptions<Region>(objectSpace)),
            LookupField("city", "City", true, cities, filterByKey: "region"),
            LookupField("type", "Type", true, types),
            LookupField("lodging", "Lodging", true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Lodging>(), row => row.FullAddress), "type", ResidenceType.Lodging.ToString(), "city"),
            LookupField("hotel", "Hotel", true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Hotel>(), row => row.Name), "type", ResidenceType.Hotel.ToString(), "city"),
            LookupField("hospital", "Hospital", true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Hospital>(), row => row.Name), "type", ResidenceType.Hospital.ToString(), "city"),
            LookupField("other", "Other", true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<OtherSite>(), row => row.FullAddress), "type", ResidenceType.Other.ToString(), "city"),
            privateHouse,
        };
    }

    private static List<PersonLinkReviewCreateOption> PlaceOptions<T>(
        Func<T, Guid?> cityId,
        IQueryable<T> query,
        Func<T, string?> label) where T : BaseObject =>
        query.AsEnumerable()
            .Select(row => new PersonLinkReviewCreateOption
            {
                Id = row.ID.ToString(),
                Label = label(row) ?? string.Empty,
                ParentId = cityId(row)?.ToString(),
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Label))
            .OrderBy(option => option.Label)
            .ToList();

    private static Passport? CreatePassport(IObjectSpace objectSpace, Person person, PersonLinkReviewCreateForm form, out string? error)
    {
        error = null;
        if (!TryDate(Value(form, "issue"), out var issue) || !TryDate(Value(form, "expiration"), out var expiration))
        {
            error = "Enter dates as dd.MM.yyyy.";
            return null;
        }
        if (expiration <= issue)
        {
            error = "Expiration date must be later than the issue date.";
            return null;
        }

        var passport = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<Passport>()
            : objectSpace.GetObjectByKey<Passport>(form.ObjectId);
        if (passport == null || (form.ObjectId != Guid.Empty && passport.Person?.ID != person.ID))
        {
            error = "This record was not found.";
            return null;
        }

        if (form.ObjectId == Guid.Empty)
            passport.Person = person;
        passport.PassportNumber = Value(form, "number");
        passport.PassportType = Lookup<PassportType>(objectSpace, Value(form, "type"));
        passport.IssuedCountry = Lookup<Country>(objectSpace, Value(form, "country"));
        passport.Authority = Value(form, "authority");
        passport.IssueDate = issue;
        passport.ExpirationDate = expiration;
        return passport;
    }

    private static Education? CreateEducation(IObjectSpace objectSpace, Person person, PersonLinkReviewCreateForm form, out string? error)
    {
        error = null;
        var education = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<Education>()
            : objectSpace.GetObjectByKey<Education>(form.ObjectId);
        if (education == null || (form.ObjectId != Guid.Empty && education.Person?.ID != person.ID))
        {
            error = "This record was not found.";
            return null;
        }

        if (form.ObjectId == Guid.Empty)
            education.Person = person;
        education.EducationLevel = Lookup<EducationLevel>(objectSpace, Value(form, "level"));
        education.EducationInstitution = Lookup<EducationInstitution>(objectSpace, Value(form, "institution"));
        education.EducationCountry = Lookup<Country>(objectSpace, Value(form, "country"));
        education.Specialty = Lookup<Specialty>(objectSpace, Value(form, "specialty"));
        return education;
    }

    private static EmployeePositionHistory? CreatePosition(IObjectSpace objectSpace, Person person, PersonLinkReviewCreateForm form, out string? error)
    {
        error = null;
        var position = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<EmployeePositionHistory>()
            : objectSpace.GetObjectByKey<EmployeePositionHistory>(form.ObjectId);
        if (position == null || (form.ObjectId != Guid.Empty && position.Person?.ID != person.ID))
        {
            error = "This record was not found.";
            return null;
        }

        if (form.ObjectId == Guid.Empty)
            position.Person = person;
        position.Position = Lookup<Position>(objectSpace, Value(form, "position"));
        position.ActualPosition = Lookup<ActualPosition>(objectSpace, Value(form, "actual"));
        position.Department = Lookup<Department>(objectSpace, Value(form, "department"));
        return position;
    }

    private static AddressOfResidence? CreateAddress(IObjectSpace objectSpace, Person person, PersonLinkReviewCreateForm form, out string? error)
    {
        error = null;
        if (!Enum.TryParse<ResidenceType>(Value(form, "type"), out var type))
        {
            error = "Type is required.";
            return null;
        }

        var address = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<AddressOfResidence>()
            : objectSpace.GetObjectByKey<AddressOfResidence>(form.ObjectId);
        if (address == null || (form.ObjectId != Guid.Empty && address.Person?.ID != person.ID))
        {
            error = "This record was not found.";
            return null;
        }

        if (form.ObjectId == Guid.Empty)
            address.Person = person;
        address.Region = Lookup<Region>(objectSpace, Value(form, "region"));
        address.City = Lookup<City>(objectSpace, Value(form, "city"));
        address.Type = type;
        address.Lodging = null;
        address.Hotel = null;
        address.Hospital = null;
        address.OtherSite = null;
        switch (type)
        {
            case ResidenceType.Lodging:
                address.Lodging = Lookup<Lodging>(objectSpace, Value(form, "lodging"));
                break;
            case ResidenceType.Hotel:
                address.Hotel = Lookup<Hotel>(objectSpace, Value(form, "hotel"));
                break;
            case ResidenceType.Hospital:
                address.Hospital = Lookup<Hospital>(objectSpace, Value(form, "hospital"));
                break;
            case ResidenceType.Other:
                address.OtherSite = Lookup<OtherSite>(objectSpace, Value(form, "other"));
                break;
            case ResidenceType.PrivateHouse:
                address.FullAddress = Value(form, "private");
                break;
        }

        return address;
    }

    private static Visa? CreateVisa(IObjectSpace objectSpace, Person person, PersonLinkReviewCreateForm form, Guid passportId, out string? error)
    {
        error = null;
        if (!TryDate(Value(form, "issue"), out var issue) || !TryDate(Value(form, "start"), out var start) || !TryDate(Value(form, "expiration"), out var expiration))
        {
            error = "Enter dates as dd.MM.yyyy.";
            return null;
        }
        if (expiration <= start)
        {
            error = "Expiration date must be later than the start date.";
            return null;
        }

        Visa? visa;
        if (form.ObjectId == Guid.Empty)
        {
            var passport = passportId == Guid.Empty ? null : objectSpace.GetObjectByKey<Passport>(passportId);
            if (passport == null || passport.Person?.ID != person.ID)
            {
                error = "Choose a passport on Overview before adding a visa.";
                return null;
            }

            visa = objectSpace.CreateObject<Visa>();
            visa.Passport = passport;
        }
        else
        {
            visa = objectSpace.GetObjectByKey<Visa>(form.ObjectId);
            if (visa == null || visa.Passport?.Person?.ID != person.ID)
            {
                error = "This record was not found.";
                return null;
            }
        }
        visa.VisaNumber = Value(form, "number");
        visa.VisaType = Lookup<VisaType>(objectSpace, Value(form, "type"));
        visa.VisaCategory = Lookup<VisaCategory>(objectSpace, Value(form, "category"));
        visa.VisaIssuedPlace = Lookup<VisaIssuedPlace>(objectSpace, Value(form, "place"));
        visa.IssueDate = issue;
        visa.StartDate = start;
        visa.ExpirationDate = expiration;
        var border = Lookup<BorderZoneName>(objectSpace, Value(form, "border"));
        visa.BorderZoneLocation = border?.NameTm;
        return visa;
    }
}
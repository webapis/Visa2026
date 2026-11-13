using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

public sealed class PersonLinkReviewLine
{
    public ApplicationProfileInstancePersonLinkKind Kind { get; init; }
    public string Label { get; init; } = string.Empty;
    /// <summary>Gear/overview tab id. Defaults to <see cref="Kind"/> when empty.</summary>
    public string TabKey { get; init; } = string.Empty;
    /// <summary>Overview-only row (e.g. Work permit location); skipped by LinkSelected.</summary>
    public bool OmitFromJoin { get; init; }
    public string Summary { get; set; } = "—";
    public Guid ObjectId { get; set; }
    public IReadOnlyList<PersonLinkReviewChoice> Choices { get; set; } = Array.Empty<PersonLinkReviewChoice>();
    public IReadOnlyList<PersonLinkReviewField> Fields { get; set; } = Array.Empty<PersonLinkReviewField>();
    /// <summary>Source-tagged location chips for requested WP/BZ location rows. Mutable Hidden for this Join.</summary>
    public List<PersonLinkReviewLocationChip> LocationChips { get; set; } = new();

    /// <summary>True when this kind stores document scans (eye shown on Overview).</summary>
    public bool SupportsDocumentCopy { get; set; }

    /// <summary>True when the selected record has at least one non-empty scan file.</summary>
    public bool HasDocumentCopy { get; set; }

    /// <summary>Person / header document-copies <c>RecordKey</c> for the selected object.</summary>
    public string? DocumentCopyRecordKey { get; set; }

    /// <summary>1-based slot when Last-N expands this kind (0 = single row).</summary>
    public int SlotIndex { get; set; }

    /// <summary>How many slots of this kind are shown (0/1 = no "n of m" caption).</summary>
    public int SlotCount { get; set; }

    public string ResolveTabKey()
    {
        if (!string.IsNullOrWhiteSpace(TabKey))
            return TabKey;
        if (SlotCount > 1 && SlotIndex > 0)
            return Kind.ToString() + ":" + SlotIndex.ToString(CultureInfo.InvariantCulture);
        return Kind.ToString();
    }
}

public sealed class PersonLinkReviewChoice
{
    public Guid ObjectId { get; init; }
    public string Label { get; init; } = "—";
    public IReadOnlyList<PersonLinkReviewField> Fields { get; init; } = Array.Empty<PersonLinkReviewField>();
    /// <summary>Not cancelled, not changed, and not expired. Manual dropdowns can hide the rest.</summary>
    public bool Active { get; init; } = true;
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
    public PersonLinkReviewChoiceRequest(ApplicationProfileInstancePersonLinkKind kind, Guid objectId, int slotIndex = 0)
    {
        Kind = kind;
        ObjectId = objectId;
        SlotIndex = slotIndex;
    }

    public ApplicationProfileInstancePersonLinkKind Kind { get; }
    public Guid ObjectId { get; }
    /// <summary>Which Last-N row changed. 0 is a single row.</summary>
    public int SlotIndex { get; }
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
public static partial class PersonLinkReview
{
    private static string T(string key) => VisaUiMessages.Get("ApplicationProfileInstance.LinkReview." + key);

    private static string U(string key) => VisaUiMessages.Get(key);

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

            if (definition.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence)
            {
                lines.Add(AddressLine(objectSpace, application, person, definition.Label));
            }
            else if (ApplicationProfilePersonLastCount.SupportsLastCount(definition.Kind))
            {
                lines.AddRange(LastNLines(objectSpace, application, person, definition.Kind, definition.Label));
            }
            else
            {
                lines.Add(Line(objectSpace, definition.Kind, definition.Label, person));
            }

            if (definition.Kind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem
                && CaseWorkPermitLocationCopy.ShowInLinkReview(application))
            {
                lines.Add(WorkPermitLocationLine(objectSpace, application, person));
            }

            if (definition.Kind == ApplicationProfileInstancePersonLinkKind.Visa
                && CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application))
            {
                lines.Add(VisaBorderZoneLocationLine(objectSpace, application, person));
            }

            if (definition.Kind == ApplicationProfileInstancePersonLinkKind.BorderZoneItem
                && CaseBorderZoneLocationCopy.ShowInLinkReview(application))
            {
                lines.Add(BorderZoneItemLocationLine(objectSpace, application, person));
            }
        }

        if (CaseVisaBorderZoneLocationCopy.ShowInLinkReview(application)
            && !lines.Any(line => string.Equals(line.ResolveTabKey(), "VisaBorderZoneLocation", StringComparison.Ordinal)))
        {
            lines.Add(VisaBorderZoneLocationLine(objectSpace, application, person));
        }

        PersonLinkReviewDocumentCopy.RefreshAll(objectSpace, lines);
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

    public static void ApplyChoice(PersonLinkReviewLine line, Guid objectId, IObjectSpace? objectSpace = null)
    {
        if (line == null)
            return;

        var choice = line.Choices.FirstOrDefault(item => item.ObjectId == objectId);
        if (choice == null)
            return;

        line.ObjectId = choice.ObjectId;
        line.Summary = string.IsNullOrWhiteSpace(choice.Label) ? "—" : choice.Label;
        line.Fields = choice.Fields;
        if (objectSpace != null)
            PersonLinkReviewDocumentCopy.Refresh(objectSpace, line);
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
        ApplyChoice(line, savedId, objectSpace);
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

        var links = ApplicationProfileInstancePersonResolver.LoadLinks(objectSpace, application, person.ID)
            .Where(row => row.LinkKind == kind
                && row.LinkedObjectId is Guid linkedId
                && linkedId != Guid.Empty)
            .ToList();

        if (links.Any(row => row.LinkedObjectId == objectId))
        {
            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace, application, person, kind, objectId);
            return;
        }

        var link = links.FirstOrDefault();
        if (link?.LinkedObjectId is not Guid previous || previous == Guid.Empty)
        {
            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace, application, person, kind, objectId);
            return;
        }

        // Single sticky slot replace — refresh concurrency token before mutating.
        if (!objectSpace.IsNewObject(link))
            objectSpace.ReloadObject(link);

        ApplicationProfileInstanceChildMembership.Remove(application, kind, previous, objectSpace);
        link.LinkedObjectId = objectId;
        ApplicationProfileInstanceChildMembership.Add(objectSpace, application, kind, objectId);
    }

    /// <summary>
    /// When the person is already on the case, show the records this case already uses.
    /// </summary>
    public static void PreferCaseLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine> lines)
    {
        if (objectSpace == null || application == null || person == null || lines == null || application.ID == Guid.Empty)
            return;

        var pinned = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID && link.PersonId == person.ID)
            .ToList();

        foreach (var group in lines.Where(line => !line.OmitFromJoin).GroupBy(line => line.Kind))
        {
            var kind = group.Key;
            var kindLines = group.OrderBy(line => line.SlotIndex).ToList();
            var pinnedIds = pinned
                .Where(link => link.LinkKind == kind && link.LinkedObjectId is Guid id && id != Guid.Empty)
                .Select(link => link.LinkedObjectId!.Value)
                .Distinct()
                .ToList();
            if (pinnedIds.Count == 0)
                continue;

            var orderedIds = OrderPinnedLikeResolve(objectSpace, application, person, kind, pinnedIds);
            for (var i = 0; i < kindLines.Count && i < orderedIds.Count; i++)
            {
                var objectId = orderedIds[i];
                var line = kindLines[i];
                var choices = line.Choices.ToList();
                if (choices.All(choice => choice.ObjectId != objectId)
                    && ChoiceForObject(objectSpace, kind, line.Label, objectId) is { } extra)
                    choices.Add(extra);

                line.Choices = choices;
                ApplyChoice(line, objectId, objectSpace);
            }
        }
    }

    public static void ApplyJoinLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine> lines)
    {
        foreach (var group in (lines ?? Array.Empty<PersonLinkReviewLine>()).Where(line => !line.OmitFromJoin).GroupBy(line => line.Kind))
        {
            if (group.Key == ApplicationProfileInstancePersonLinkKind.AddressOfResidence)
            {
                foreach (var line in group)
                {
                    if (line.ObjectId == Guid.Empty)
                        InvitationStayAddressCopy.AcceptOnJoin(objectSpace, application, person);
                    else
                        LinkSelected(objectSpace, application, person, line.Kind, line.ObjectId);
                }

                continue;
            }

            var ids = group
                .OrderBy(line => line.SlotIndex)
                .Select(line => line.ObjectId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                continue;

            if (ApplicationProfilePersonLastCount.SupportsLastCount(group.Key)
                && ApplicationProfilePersonLastCount.For(application, group.Key) > 1)
            {
                ApplicationProfileInstancePersonResolver.SyncResolvedLinksForKind(
                    objectSpace, application, person, group.Key, ids);
            }
            else
            {
                LinkSelected(objectSpace, application, person, group.Key, ids[0]);
            }
        }

        CaseWorkPermitLocationCopy.AcceptOnJoin(objectSpace, application, person);
        CaseBorderZoneLocationCopy.AcceptOnJoin(objectSpace, application, person);
        CaseVisaBorderZoneLocationCopy.AcceptOnJoin(objectSpace, application, person);

        // Only push chip edits. An empty chip list means "no officer edits" — not "clear
        // locations". Treating "—" as clear deleted AcceptOnJoin rows and tripped
        // OptimisticLockField (expected 1 row, affected 0) on Join/Bagla.
        foreach (var line in lines ?? Array.Empty<PersonLinkReviewLine>())
        {
            if (!line.OmitFromJoin)
                continue;
            if (!string.Equals(line.ResolveTabKey(), "WorkPermitLocation", StringComparison.Ordinal)
                && !string.Equals(line.ResolveTabKey(), "BorderZoneLocation", StringComparison.Ordinal)
                && !string.Equals(line.ResolveTabKey(), "VisaBorderZoneLocation", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.LocationChips == null || line.LocationChips.Count == 0)
                continue;

            var merged = LinkReviewLocationChips.JoinVisible(line.LocationChips);
            line.Summary = string.IsNullOrWhiteSpace(merged) ? "—" : merged;

            if (string.Equals(line.ResolveTabKey(), "WorkPermitLocation", StringComparison.Ordinal))
            {
                CaseWorkPermitLocationCopy.TryApplyEditedLocations(
                    objectSpace, application, person, line.ObjectId, merged);
            }
            else if (string.Equals(line.ResolveTabKey(), "BorderZoneLocation", StringComparison.Ordinal))
            {
                CaseBorderZoneLocationCopy.TryApplyEditedLocations(
                    objectSpace, application, person, line.ObjectId, merged);
            }
            else
            {
                CaseVisaBorderZoneLocationCopy.TryApplyEditedLocations(
                    objectSpace, application, person, line.ObjectId, merged);
            }
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

    private static PersonLinkReviewLine WorkPermitLocationLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        var item = ResolveLinkedWorkPermitItem(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person);
        var visa = ResolveLinkedVisa(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveVisa(person);
        var caseLocations = CaseWorkPermitLocationCopy.ResolveCaseLocations(application);
        var joinedLocations = CasePersonRequestedLocation.GetLocations(
            objectSpace,
            application.ID,
            person.ID,
            ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit);
        var itemLocations = item?.WorkPermittedLocations?.Trim();
        var visaLocations = BorderZoneSelectionHelper.IsNoneValue(visa?.BorderZoneLocation)
            ? null
            : visa?.BorderZoneLocation?.Trim();
        var chips = LinkReviewLocationChips.Build(joinedLocations, caseLocations, visaLocations, itemLocations).ToList();
        var summary = LinkReviewLocationChips.DisplayPreferJoined(joinedLocations, chips);

        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.WorkPermitItem,
            TabKey = "WorkPermitLocation",
            OmitFromJoin = true,
            Label = "Work permit location",
            Summary = summary,
            ObjectId = item?.ID ?? Guid.Empty,
            LocationChips = chips,
        };

        line.Fields = new List<PersonLinkReviewField>
        {
            Field(ApplicationProfileLocalization.Field("WorkPermitLocation"), summary, true, false),
        };
        line.Choices = Array.Empty<PersonLinkReviewChoice>();
        return line;
    }

    private static WorkPermitItem? ResolveLinkedWorkPermitItem(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return null;

        var linkedId = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == person.ID
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId)
            .FirstOrDefault();
        if (linkedId is not Guid id || id == Guid.Empty)
            return null;

        return objectSpace.GetObjectByKey<WorkPermitItem>(id);
    }
    private static PersonLinkReviewLine VisaBorderZoneLocationLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        var visa = ResolveLinkedVisa(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveVisa(person);
        var wpItem = ResolveLinkedWorkPermitItem(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person);
        var caseLocations = CaseVisaBorderZoneLocationCopy.ResolveCaseLocations(application);
        var joinedLocations = CasePersonRequestedLocation.GetLocations(
            objectSpace,
            application.ID,
            person.ID,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone);
        var visaLocations = BorderZoneSelectionHelper.IsNoneValue(visa?.BorderZoneLocation)
            ? null
            : visa?.BorderZoneLocation?.Trim();
        var wpLocations = wpItem?.WorkPermittedLocations?.Trim();
        var chips = LinkReviewLocationChips.Build(joinedLocations, caseLocations, visaLocations, wpLocations).ToList();
        var summary = LinkReviewLocationChips.DisplayPreferJoined(joinedLocations, chips);

        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.Visa,
            TabKey = "VisaBorderZoneLocation",
            OmitFromJoin = true,
            Label = "Border zone location",
            Summary = summary,
            ObjectId = visa?.ID ?? Guid.Empty,
            LocationChips = chips,
        };

        line.Fields = new List<PersonLinkReviewField>
        {
            Field(ApplicationProfileLocalization.Field("BorderZoneLocation"), summary, true, false),
        };
        line.Choices = Array.Empty<PersonLinkReviewChoice>();
        return line;
    }

    private static Visa? ResolveLinkedVisa(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return null;

        var linkedId = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == person.ID
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.Visa
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId)
            .FirstOrDefault();
        if (linkedId is not Guid id || id == Guid.Empty)
            return null;

        return objectSpace.GetObjectByKey<Visa>(id);
    }
    private static PersonLinkReviewLine BorderZoneItemLocationLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        var item = ResolveLinkedBorderZoneItem(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveBorderZoneItem(objectSpace, person);
        var visa = ResolveLinkedVisa(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveVisa(person);
        var wpItem = ResolveLinkedWorkPermitItem(objectSpace, application, person)
            ?? ApplicationProfileInstancePersonValidItems.ResolveWorkPermitItem(person);
        var caseLocations = CaseBorderZoneLocationCopy.ResolveCaseLocations(application);
        var joinedLocations = CasePersonRequestedLocation.GetLocations(
            objectSpace,
            application.ID,
            person.ID,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone);
        var visaLocations = BorderZoneSelectionHelper.IsNoneValue(visa?.BorderZoneLocation)
            ? null
            : visa?.BorderZoneLocation?.Trim();
        var itemLocations = BorderZoneSelectionHelper.IsNoneValue(item?.BorderZoneLocation)
            ? null
            : item?.BorderZoneLocation?.Trim();
        var chips = LinkReviewLocationChips.Build(joinedLocations, caseLocations, visaLocations, itemLocations).ToList();
        // Only suggest WP tokens before the first Join; after Join chips are the persisted row only.
        if (string.IsNullOrWhiteSpace(joinedLocations))
        {
            foreach (var token in SplitLocationTokens(wpItem?.WorkPermittedLocations))
            {
                if (chips.Any(c => string.Equals(c.Value, token, StringComparison.Ordinal)))
                    continue;
                chips.Add(new PersonLinkReviewLocationChip
                {
                    Value = token,
                    Source = LinkReviewLocationChipSource.WorkPermitItem,
                });
            }
        }
        var summary = LinkReviewLocationChips.DisplayPreferJoined(joinedLocations, chips);

        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.BorderZoneItem,
            TabKey = "BorderZoneLocation",
            OmitFromJoin = true,
            Label = "Border zone location",
            Summary = summary,
            ObjectId = item?.ID ?? Guid.Empty,
            LocationChips = chips,
        };

        line.Fields = new List<PersonLinkReviewField>
        {
            Field(ApplicationProfileLocalization.Field("BorderZoneLocation"), summary, true, false),
        };
        line.Choices = Array.Empty<PersonLinkReviewChoice>();
        return line;
    }

    private static IEnumerable<string> SplitLocationTokens(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored) || stored.Trim() == "—")
            yield break;
        foreach (var part in stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(part))
                yield return part.Trim();
        }
    }

    private static BorderZoneItem? ResolveLinkedBorderZoneItem(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        if (objectSpace == null || application == null || person == null || person.ID == Guid.Empty)
            return null;

        var linkedId = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(link => link.ApplicationProfileInstanceId == application.ID
                && link.PersonId == person.ID
                && link.LinkKind == ApplicationProfileInstancePersonLinkKind.BorderZoneItem
                && link.LinkedObjectId != null
                && link.LinkedObjectId != Guid.Empty)
            .Select(link => link.LinkedObjectId)
            .FirstOrDefault();
        if (linkedId is not Guid id || id == Guid.Empty)
            return null;

        return objectSpace.GetObjectByKey<BorderZoneItem>(id);
    }
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
        ApplicationProfileInstancePersonLinkKind.Visa => VisaChoices(
            objectSpace.GetObjectsQuery<Visa>()
                .Where(row => row.Passport != null && row.Passport.Person != null && row.Passport.Person.ID == personId)
                .ToList()),
        ApplicationProfileInstancePersonLinkKind.WorkPermitItem => WorkPermitChoices(
            objectSpace.GetObjectsQuery<WorkPermitItem>()
                .Where(row => row.Person != null && row.Person.ID == personId)
                .ToList()),
        _ => Array.Empty<PersonLinkReviewChoice>(),
    };

    /// <summary>
    /// Every visa for the manual dropdown. <see cref="PersonLinkReviewChoice.Active"/> is the default filter.
    /// Last-N still decides which visa is selected until the officer picks another.
    /// </summary>
    public static IReadOnlyList<PersonLinkReviewChoice> VisaChoices(IEnumerable<Visa>? items) =>
        (items ?? Array.Empty<Visa>())
            .OrderByDescending(row => row.StartDate)
            .ThenByDescending(row => row.ID)
            .Select(row => Choice(
                row.ID,
                VisaSummary(row).Summary,
                VisaFields(row),
                ApplicationProfileInstancePersonValidItems.CanLinkVisa(row)))
            .ToList();

    /// <summary>
    /// Every work permit for the manual dropdown, newest start first.
    /// <see cref="PersonLinkReviewChoice.Active"/> is not cancelled, not changed, and not expired.
    /// Last-N still decides which permit is selected until the officer picks another.
    /// </summary>
    public static IReadOnlyList<PersonLinkReviewChoice> WorkPermitChoices(IEnumerable<WorkPermitItem>? items) =>
        (items ?? Array.Empty<WorkPermitItem>())
            .OrderByDescending(row => row.StartDate)
            .ThenByDescending(row => row.ID)
            .Select(row => Choice(
                row.ID,
                WorkPermitSummary(row).Summary,
                WorkPermitFields(row),
                ApplicationProfileInstancePersonValidItems.CanLinkWorkPermitItem(row)))
            .ToList();

    public static IReadOnlyList<PersonLinkReviewChoice> ActiveWorkPermitChoices(IEnumerable<WorkPermitItem>? items) =>
        WorkPermitChoices(items).Where(choice => choice.Active).ToList();

    /// <summary>Visa and work permit rows can be narrowed while the officer picks by hand.</summary>
    public static bool ManualActiveFilterApplies(PersonLinkReviewLine? line) =>
        line != null
        && !line.OmitFromJoin
        && line.Kind is ApplicationProfileInstancePersonLinkKind.Visa
            or ApplicationProfileInstancePersonLinkKind.WorkPermitItem;

    /// <summary>
    /// Gear-on dropdown. Active-only keeps active rows and the value already selected.
    /// Other record types, and an unchecked box, list every choice.
    /// </summary>
    public static IReadOnlyList<PersonLinkReviewChoice> ChoicesForManualPick(PersonLinkReviewLine? line, bool activeOnly)
    {
        if (line == null)
            return Array.Empty<PersonLinkReviewChoice>();
        if (!activeOnly || !ManualActiveFilterApplies(line))
            return line.Choices;

        return line.Choices
            .Where(choice => choice.Active || choice.ObjectId == line.ObjectId)
            .ToList();
    }

    public static PersonLinkReviewLine? FindChoiceLine(
        IEnumerable<PersonLinkReviewLine>? lines,
        PersonLinkReviewChoiceRequest? request)
    {
        if (lines == null || request == null)
            return null;

        return lines.FirstOrDefault(item => item.Kind == request.Kind && item.SlotIndex == request.SlotIndex);
    }

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
                return visa == null ? null : VisaChoices(new[] { visa }).FirstOrDefault();
            case ApplicationProfileInstancePersonLinkKind.WorkPermitItem:
                var workPermit = objectSpace.GetObjectByKey<WorkPermitItem>(objectId);
                return workPermit == null ? null : WorkPermitChoices(new[] { workPermit }).FirstOrDefault();
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

    private static PersonLinkReviewChoice Choice(
        Guid objectId,
        string? label,
        IReadOnlyList<PersonLinkReviewField> fields,
        bool active = true) =>
        new()
        {
            ObjectId = objectId,
            Label = string.IsNullOrWhiteSpace(label) ? "—" : label,
            Fields = fields,
            Active = active,
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
        Field(U("ApplicationProfileInstance.Org.PassportNumber"), passport.PassportNumber, true, false),
        Field(T("PassportType"), passport.PassportType?.NameTm, true, true),
        Field(T("IssuedCountry"), passport.IssuedCountry?.NameTm, true, true),
        Field(T("Authority"), passport.Authority, true, false),
        Field(T("IssueDate"), DateText(passport.IssueDate), true, false),
        Field(U("ApplicationProfileInstance.Workspace.AddressExpiration"), DateText(passport.ExpirationDate), true, false),
    };

    private static IReadOnlyList<PersonLinkReviewField> EducationFields(Education education) => new[]
    {
        Field(T("EducationLevel"), education.EducationLevel?.NameTm, true, true),
        Field(T("Institution"), education.EducationInstitution?.NameTm, true, true),
        Field(T("Country"), education.EducationCountry?.NameTm, true, true),
        Field(T("Specialty"), education.Specialty?.NameTm, true, true),
    };

    private static IReadOnlyList<PersonLinkReviewField> PositionFields(EmployeePositionHistory position) => new[]
    {
        Field(T("PositionReports"), position.Position?.NameTm, true, true),
        Field(T("PositionActual"), position.ActualPosition?.Name, true, true),
        Field(T("Department"), position.Department?.NameTm, false, true),
    };

    private static IReadOnlyList<PersonLinkReviewField> WorkPermitFields(WorkPermitItem item) => new[]
    {
        Field(U("ApplicationProfileInstance.Issued.WorkPermitNumber"), item.WorkPermitNumber, true, false),
        Field(U("ApplicationProfile.Field.StartDate"), DateText(item.StartDate), true, false),
        Field(U("ApplicationProfileInstance.Workspace.AddressExpiration"), DateText(item.ExpirationDate), true, false),
    };

    private static IReadOnlyList<PersonLinkReviewField> VisaFields(Visa visa) => new[]
    {
        Field(T("VisaNumber"), visa.VisaNumber, true, false),
        Field(T("VisaType"), visa.VisaType?.NameTm, true, true),
        Field(T("VisaCategory"), visa.VisaCategory?.NameTm, true, true),
        Field(T("IssuedPlace"), visa.VisaIssuedPlace?.NameTm, true, true),
        Field(T("IssueDate"), DateText(visa.IssueDate), true, false),
        Field(U("ApplicationProfile.Field.StartDate"), DateText(visa.StartDate), true, false),
        Field(U("ApplicationProfileInstance.Workspace.AddressExpiration"), DateText(visa.ExpirationDate), true, false),
        Field(T("DaysRemaining"), visa.DaysRemaining.ToString(CultureInfo.InvariantCulture), false, false),
        Field(U("ApplicationProfile.Doc.BorderZone"), visa.BorderZoneLocation, true, false),
    };

    private static IReadOnlyList<PersonLinkReviewField> StayFields(InvitationAddress stay) => new[]
    {
        Field(T("Region"), stay.Region?.NameTm, true, true),
        Field(T("City"), stay.City?.NameTm, true, true),
        Field(T("Type"), TypeLabel(stay.Type), true, true),
        Field(PlaceLabel(stay.Type), Place(stay), true, stay.Type != ResidenceType.PrivateHouse),
    };

    private static IReadOnlyList<PersonLinkReviewField> AddressFields(AddressOfResidence address) => new[]
    {
        Field(T("Region"), address.Region?.NameTm, true, true),
        Field(T("City"), address.City?.NameTm, true, true),
        Field(T("Type"), TypeLabel(address.Type), true, true),
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
        ResidenceType.Hotel => U("ApplicationProfile.Site.Hotel"),
        ResidenceType.Hospital => U("ApplicationProfile.Site.Hospital"),
        ResidenceType.Other => U("ApplicationProfile.Site.Other"),
        ResidenceType.PrivateHouse => U("ApplicationProfile.Site.PrivateHouse"),
        _ => U("ApplicationProfile.Site.Lodging"),
    };

    private static (string Summary, Guid? Id) PassportSummary(Passport? passport) =>
        (JoinParts(passport?.PassportNumber, passport?.PassportType?.NameTm, passport?.IssuedCountry?.NameTm, DateText(passport?.ExpirationDate)), passport?.ID);

    private static (string Summary, Guid? Id) EducationSummary(Education? education) =>
        (JoinParts(education?.EducationLevel?.NameTm, education?.EducationInstitution?.NameTm, education?.Specialty?.NameTm), education?.ID);

    private static (string Summary, Guid? Id) PositionSummary(EmployeePositionHistory? position) =>
        (JoinParts(position?.Position?.NameTm, position?.Department?.NameTm, DateText(position?.StartDate)), position?.ID);

    private static (string Summary, Guid? Id) VisaSummary(Visa? visa) =>
        (JoinParts(
            visa?.VisaNumber,
            visa?.VisaType?.NameTm,
            visa?.VisaCategory?.NameTm,
            DateRangeText(visa?.StartDate, visa?.ExpirationDate)), visa?.ID);

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
        ResidenceType.Lodging => U("ApplicationProfile.Site.Lodging"),
        ResidenceType.Hotel => U("ApplicationProfile.Site.Hotel"),
        ResidenceType.Hospital => U("ApplicationProfile.Site.Hospital"),
        ResidenceType.Other => U("ApplicationProfile.Site.Other"),
        ResidenceType.PrivateHouse => U("ApplicationProfile.Site.PrivateHouse"),
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

    private static string? DateRangeText(DateTime? start, DateTime? end)
    {
        var startText = DateText(start);
        var endText = DateText(end);
        if (string.IsNullOrEmpty(startText) && string.IsNullOrEmpty(endText))
            return null;
        if (string.IsNullOrEmpty(startText))
            return endText;
        if (string.IsNullOrEmpty(endText))
            return startText;
        return startText + " – " + endText;
    }

    public static PersonLinkReviewCreateForm BuildCreateForm(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonLinkKind kind,
        string title)
    {
        var form = new PersonLinkReviewCreateForm
        {
            Kind = kind,
            Title = ApplicationProfileLocalization.LinkedRecordLabel(title),
        };
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
            form.Error = T("CannotAdd");
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
            form.Error = T("NotFound");
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
            error = T("CouldNotSave");
            return Guid.Empty;
        }

        var missing = form.Fields.FirstOrDefault(field => field.Required && IsShown(form, field) && string.IsNullOrWhiteSpace(field.Value));
        if (missing != null)
        {
            error = VisaUiMessages.Format("ApplicationProfileInstance.LinkReview.Required", missing.Label);
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
                ? T("CannotAdd")
                : T("CannotEdit");
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
        TextField("number", U("ApplicationProfileInstance.Org.PassportNumber"), true),
        LookupField("type", T("PassportType"), true, CatalogOptions<PassportType>(objectSpace)),
        LookupField("country", T("IssuedCountry"), true, CatalogOptions<Country>(objectSpace)),
        TextField("authority", T("Authority"), true),
        TextField("issue", T("IssueDate"), true, date: true),
        TextField("expiration", U("ApplicationProfileInstance.Workspace.AddressExpiration"), true, date: true),
    };

    private static List<PersonLinkReviewCreateField> EducationCreateFields(IObjectSpace objectSpace) => new()
    {
        LookupField("level", T("EducationLevel"), true, CatalogOptions<EducationLevel>(objectSpace)),
        LookupField("institution", T("Institution"), true, CatalogOptions<EducationInstitution>(objectSpace)),
        LookupField("country", T("Country"), true, CatalogOptions<Country>(objectSpace)),
        LookupField("specialty", T("Specialty"), true, CatalogOptions<Specialty>(objectSpace)),
    };

    private static List<PersonLinkReviewCreateField> PositionCreateFields(IObjectSpace objectSpace) => new()
    {
        LookupField("position", T("PositionReports"), true, CatalogOptions<Position>(objectSpace)),
        LookupField("actual", T("PositionActual"), true, ActualPositionOptions(objectSpace)),
        LookupField("department", T("Department"), false, CatalogOptions<Department>(objectSpace)),
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
        TextField("number", T("VisaNumber"), true),
        LookupField("type", T("VisaType"), true, CatalogOptions<VisaType>(objectSpace)),
        LookupField("category", T("VisaCategory"), true, CatalogOptions<VisaCategory>(objectSpace)),
        LookupField("place", T("IssuedPlace"), true, CatalogOptions<VisaIssuedPlace>(objectSpace)),
        TextField("issue", T("IssueDate"), true, date: true),
        TextField("start", U("ApplicationProfile.Field.StartDate"), true, date: true),
        TextField("expiration", U("ApplicationProfileInstance.Workspace.AddressExpiration"), true, date: true),
        LookupField("border", U("ApplicationProfile.Doc.BorderZone"), true, CatalogOptions<BorderZoneName>(objectSpace)),
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
        var privateHouse = TextField("private", U("ApplicationProfile.Site.PrivateHouse"), true);
        privateHouse.ShowWhenKey = "type";
        privateHouse.ShowWhenValue = ResidenceType.PrivateHouse.ToString();
        return new List<PersonLinkReviewCreateField>
        {
            LookupField("region", T("Region"), true, CatalogOptions<Region>(objectSpace)),
            LookupField("city", T("City"), true, cities, filterByKey: "region"),
            LookupField("type", T("Type"), true, types),
            LookupField("lodging", U("ApplicationProfile.Site.Lodging"), true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Lodging>(), row => row.FullAddress), "type", ResidenceType.Lodging.ToString(), "city"),
            LookupField("hotel", U("ApplicationProfile.Site.Hotel"), true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Hotel>(), row => row.Name), "type", ResidenceType.Hotel.ToString(), "city"),
            LookupField("hospital", U("ApplicationProfile.Site.Hospital"), true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<Hospital>(), row => row.Name), "type", ResidenceType.Hospital.ToString(), "city"),
            LookupField("other", U("ApplicationProfile.Site.Other"), true, PlaceOptions(row => row.City?.ID, objectSpace.GetObjectsQuery<OtherSite>(), row => row.FullAddress), "type", ResidenceType.Other.ToString(), "city"),
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
            error = T("EnterDates");
            return null;
        }
        if (expiration <= issue)
        {
            error = T("ExpirationAfterIssue");
            return null;
        }

        var passport = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<Passport>()
            : objectSpace.GetObjectByKey<Passport>(form.ObjectId);
        if (passport == null || (form.ObjectId != Guid.Empty && passport.Person?.ID != person.ID))
        {
            error = T("NotFound");
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
            error = T("NotFound");
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
            error = T("NotFound");
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
            error = T("TypeRequired");
            return null;
        }

        var address = form.ObjectId == Guid.Empty
            ? objectSpace.CreateObject<AddressOfResidence>()
            : objectSpace.GetObjectByKey<AddressOfResidence>(form.ObjectId);
        if (address == null || (form.ObjectId != Guid.Empty && address.Person?.ID != person.ID))
        {
            error = T("NotFound");
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
            error = T("EnterDates");
            return null;
        }
        if (expiration <= start)
        {
            error = T("ExpirationAfterStart");
            return null;
        }

        Visa? visa;
        if (form.ObjectId == Guid.Empty)
        {
            var passport = passportId == Guid.Empty ? null : objectSpace.GetObjectByKey<Passport>(passportId);
            if (passport == null || passport.Person?.ID != person.ID)
            {
                error = T("ChoosePassportFirst");
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
                error = T("NotFound");
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
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services;

public static partial class PersonLinkReview
{
    private static IReadOnlyList<PersonLinkReviewLine> AddressSlotLines(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string fallbackLabel)
    {
        var maxSlots = ApplicationProfilePersonAddressMaxCount.For(application);
        if (maxSlots <= 0)
            return Array.Empty<PersonLinkReviewLine>();

        if (maxSlots == 1)
        {
            var single = AddressSlotLine(objectSpace, application, person, slot: 1, label: fallbackLabel);
            single.SlotIndex = 0;
            single.SlotCount = 0;
            return new[] { single };
        }

        var slot1 = AddressSlotLine(objectSpace, application, person, slot: 1, label: "Address 1");
        slot1.SlotIndex = 1;
        slot1.SlotCount = maxSlots;

        var slot2 = AddressSlotLine(objectSpace, application, person, slot: 2, label: "Address 2");
        slot2.SlotIndex = 2;
        slot2.SlotCount = maxSlots;
        return new[] { slot1, slot2 };
    }

    private static PersonLinkReviewLine AddressSlotLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        int slot,
        string label)
    {
        var resolved = ApplicationProfileInstanceAddressResolver.ResolveAddressSlot(
            application, person, slot, objectSpace);
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            Label = label,
            Summary = AddressSummary(resolved),
            ObjectId = resolved?.ID ?? Guid.Empty,
        };
        Attach(line, AddressChoicesForCase(objectSpace, application, person, slot, excludeObjectId: Guid.Empty));
        return line;
    }

    /// <summary>
    /// Rebuild Address 2 choices with case context (instance Address1/2 + person overrides).
    /// </summary>
    public static void RefreshAddressSlot2Choices(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine>? lines)
    {
        if (objectSpace == null || application == null || person == null || lines == null)
            return;

        var slot1 = lines.FirstOrDefault(line =>
            line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
            && line.SlotIndex == 1);
        var slot2 = lines.FirstOrDefault(line =>
            line.Kind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
            && line.SlotIndex == 2);
        if (slot2 == null)
            return;

        var exclude = slot1?.ObjectId ?? Guid.Empty;
        var selected = slot2.ObjectId;
        if (selected != Guid.Empty && selected == exclude)
        {
            slot2.ObjectId = Guid.Empty;
            slot2.Summary = "—";
            selected = Guid.Empty;
        }

        var choices = AddressChoicesForCase(objectSpace, application, person, slot: 2, excludeObjectId: exclude).ToList();
        if (selected != Guid.Empty
            && choices.All(choice => choice.ObjectId != selected)
            && objectSpace.GetObjectByKey<AddressOfResidence>(selected) is { } extra)
        {
            choices.Insert(0, Choice(extra.ID, AddressChoiceLabelCaseOwned(extra, isCaseDefault: false), AddressFields(extra)));
        }

        slot2.Choices = choices;
        if (selected != Guid.Empty)
            ApplyChoice(slot2, selected, objectSpace);
        else
            slot2.Fields = Array.Empty<PersonLinkReviewField>();
    }

    private static void PreferCaseAddressLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine> addressLines,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> pinned)
    {
        var addressPinned = pinned
            .Where(link => link.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                && link.LinkedObjectId is Guid id
                && id != Guid.Empty)
            .OrderBy(link => link.LinkSlotIndex)
            .ToList();
        if (addressPinned.Count == 0)
        {
            RefreshAddressSlot2Choices(objectSpace, application, person, addressLines);
            return;
        }

        foreach (var line in addressLines.OrderBy(l => l.SlotIndex))
        {
            var slot = line.SlotIndex > 0 ? line.SlotIndex : 1;
            var match = addressPinned.FirstOrDefault(link => link.LinkSlotIndex == slot)
                ?? (slot == 1 ? addressPinned.FirstOrDefault() : null);
            if (match?.LinkedObjectId is not Guid objectId || objectId == Guid.Empty)
                continue;

            var choices = AddressChoicesForCase(objectSpace, application, person, slot, excludeObjectId: Guid.Empty).ToList();
            if (choices.All(choice => choice.ObjectId != objectId)
                && objectSpace.GetObjectByKey<AddressOfResidence>(objectId) is { } extra)
            {
                choices.Add(Choice(extra.ID, AddressChoiceLabelCaseOwned(extra, isCaseDefault: false), AddressFields(extra)));
            }

            line.Choices = choices;
            ApplyChoice(line, objectId, objectSpace);
        }

        RefreshAddressSlot2Choices(objectSpace, application, person, addressLines);
    }

    private static void ApplyJoinAddressLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IEnumerable<PersonLinkReviewLine> addressLines)
    {
        var list = addressLines.OrderBy(l => l.SlotIndex).ToList();
        Guid slot1Id = Guid.Empty;
        foreach (var line in list)
        {
            var slot = line.SlotIndex > 0 ? line.SlotIndex : 1;
            if (line.ObjectId == Guid.Empty)
            {
                // Inherit case summary Address1/2 — no Person child copy.
                var summary = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, slot);
                if (summary != null && summary.ID != Guid.Empty)
                {
                    // Default = no override ResolvedLink; leave empty so hydrate uses instance FK.
                    continue;
                }

                continue;
            }

            var summaryId = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, slot)?.ID;
            if (summaryId == line.ObjectId)
            {
                // Selecting case default clears override.
                continue;
            }

            if (slot == 1)
                slot1Id = line.ObjectId;
            else if (slot >= 2 && line.ObjectId == slot1Id)
                continue;

            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace, application, person, line.Kind, line.ObjectId, slot);
        }
    }

    /// <summary>
    /// Choices = instance Address1/2 labeled Case default + this person's override rows for the case.
    /// </summary>
    private static IReadOnlyList<PersonLinkReviewChoice> AddressChoicesForCase(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        int slot,
        Guid excludeObjectId)
    {
        var choices = new List<PersonLinkReviewChoice>
        {
            Choice(Guid.Empty, "—", Array.Empty<PersonLinkReviewField>())
        };

        var caseDefaultLabel = ApplicationProfileLocalization.Field("AddressCaseDefault");
        if (string.IsNullOrWhiteSpace(caseDefaultLabel) || caseDefaultLabel.StartsWith("ApplicationProfile.", StringComparison.Ordinal))
            caseDefaultLabel = "Case default";

        var summary = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, slot);
        if (summary != null && summary.ID != Guid.Empty && summary.ID != excludeObjectId)
        {
            choices.Add(Choice(
                summary.ID,
                caseDefaultLabel + " — " + AddressSummary(summary),
                AddressFields(summary)));
        }

        // Other summary slot as optional pick when max is 2.
        var otherSlot = slot >= 2 ? 1 : 2;
        if (ApplicationProfilePersonAddressMaxCount.For(application) >= 2)
        {
            var other = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, otherSlot);
            if (other != null && other.ID != Guid.Empty && other.ID != excludeObjectId
                && other.ID != summary?.ID
                && choices.All(c => c.ObjectId != other.ID))
            {
                choices.Add(Choice(
                    other.ID,
                    caseDefaultLabel + " — " + AddressSummary(other),
                    AddressFields(other)));
            }
        }

        var overrides = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(row => row.OwningApplicationProfileInstance != null
                && row.OwningApplicationProfileInstance.ID == application.ID
                && row.RosterPerson != null
                && row.RosterPerson.ID == person.ID)
            .OrderByDescending(row => row.ID)
            .AsEnumerable()
            .Where(row => row.ID != excludeObjectId && row.ID != summary?.ID)
            .ToList();

        foreach (var row in overrides)
        {
            if (choices.Any(c => c.ObjectId == row.ID))
                continue;
            choices.Add(Choice(row.ID, AddressChoiceLabelCaseOwned(row, isCaseDefault: false), AddressFields(row)));
        }

        // Same shared catalog as Case summary / create-wizard Address picker (searchable).
        AppendSharedAddressCatalog(choices, objectSpace, excludeObjectId);
        return choices;
    }

    private static void AppendSharedAddressCatalog(
        List<PersonLinkReviewChoice> choices,
        IObjectSpace objectSpace,
        Guid excludeObjectId)
    {
        var seen = new HashSet<Guid>(choices.Select(c => c.ObjectId));
        var rows = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .AsNoTracking()
            .Include(row => row.Region)
            .Include(row => row.City)
            .Include(row => row.Lodging)
            .Include(row => row.Hotel)
            .Include(row => row.Hospital)
            .Include(row => row.OtherSite)
            .Where(row => row.RosterPerson == null)
            .OrderByDescending(row => row.ID)
            .Take(400)
            .AsEnumerable()
            .ToList();

        foreach (var row in rows)
        {
            if (row.ID == Guid.Empty || row.ID == excludeObjectId || !seen.Add(row.ID))
                continue;

            var label = AddressOfResidenceReportText.CityAndStreet(row);
            if (string.IsNullOrWhiteSpace(label))
                label = row.DisplayAddress;
            if (string.IsNullOrWhiteSpace(label))
                label = AddressSummary(row);
            choices.Add(Choice(row.ID, label, AddressFields(row)));
        }
    }

    private static string AddressChoiceLabelCaseOwned(AddressOfResidence address, bool isCaseDefault)
    {
        var summary = AddressSummary(address);
        if (isCaseDefault)
        {
            var prefix = ApplicationProfileLocalization.Field("AddressCaseDefault");
            if (string.IsNullOrWhiteSpace(prefix) || prefix.StartsWith("ApplicationProfile.", StringComparison.Ordinal))
                prefix = "Case default";
            if (string.IsNullOrWhiteSpace(summary) || summary == "—")
                return prefix;
            return prefix + " — " + summary;
        }

        return summary;
    }
}
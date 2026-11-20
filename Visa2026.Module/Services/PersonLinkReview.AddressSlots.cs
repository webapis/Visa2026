using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ReportDashboard;

namespace Visa2026.Module.Services;

public static partial class PersonLinkReview
{
    internal static IReadOnlyList<PersonLinkReviewLine> AddressSlotLines(
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

        var slot2 = Address2TextNoteLine(objectSpace, application, person);
        slot2.SlotIndex = 2;
        slot2.SlotCount = maxSlots;
        return new[] { slot1, slot2 };
    }

    internal static PersonLinkReviewLine Address2TextNoteLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        var text = CasePersonAddress2Note.GetText(objectSpace, application.ID, person.ID);
        // Prefill UI from case default when person has no note (copy-on-Join writes it).
        if (string.IsNullOrWhiteSpace(text))
            text = CasePersonAddress2Recent.NormalizeDisplay(application.Address2DefaultText);

        var summary = string.IsNullOrWhiteSpace(text) ? "—" : text!;
        return new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            Label = "Address 2",
            Summary = summary,
            ObjectId = Guid.Empty,
            UsesMultilineText = true,
            TextValue = text,
            Choices = Array.Empty<PersonLinkReviewChoice>(),
            Fields = Array.Empty<PersonLinkReviewField>(),
        };
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
    /// Obsolete: Address 2 is free text. Kept as no-op for older call sites.
    /// </summary>
    public static void RefreshAddressSlot2Choices(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IReadOnlyList<PersonLinkReviewLine>? lines)
    {
        _ = objectSpace;
        _ = application;
        _ = person;
        _ = lines;
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
            return;

        foreach (var line in addressLines.Where(l => !l.UsesMultilineText).OrderBy(l => l.SlotIndex))
        {
            var slot = line.SlotIndex > 0 ? line.SlotIndex : 1;
            if (slot >= 2)
                continue;

            var match = addressPinned.FirstOrDefault(link => link.LinkSlotIndex == slot)
                ?? addressPinned.FirstOrDefault();
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
    }

    private static void ApplyJoinAddressLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IEnumerable<PersonLinkReviewLine> addressLines)
    {
        var list = addressLines.OrderBy(l => l.SlotIndex).ToList();
        foreach (var line in list)
        {
            if (line.UsesMultilineText)
            {
                var incoming = CasePersonAddress2Recent.NormalizeDisplay(line.TextValue);
                var existing = CasePersonAddress2Note.GetText(objectSpace, application.ID, person.ID);
                if (incoming == null && existing == null)
                    incoming = CasePersonAddress2Recent.NormalizeDisplay(application.Address2DefaultText);

                CasePersonAddress2Note.Upsert(objectSpace, application, person, incoming);
                CasePersonAddress2Recent.Remember(objectSpace, application, incoming);
                continue;
            }

            var slot = line.SlotIndex > 0 ? line.SlotIndex : 1;
            if (slot >= 2)
                continue;

            if (line.ObjectId == Guid.Empty)
            {
                // Inherit case summary Address1 — no Person child copy / override link.
                continue;
            }

            var summaryId = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, slot)?.ID;
            if (summaryId == line.ObjectId)
            {
                // Selecting case default clears override.
                continue;
            }

            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace, application, person, line.Kind, line.ObjectId, slot);
        }
    }

    /// <summary>
    /// Choices = instance Address1 labeled Case default + shared catalog (Address 1 only).
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

        // Address 2 is free text — catalog choices only for Address 1.
        if (slot >= 2)
            return choices;

        var caseDefaultLabel = ApplicationProfileLocalization.Field("AddressCaseDefault");
        if (string.IsNullOrWhiteSpace(caseDefaultLabel) || caseDefaultLabel.StartsWith("ApplicationProfile.", StringComparison.Ordinal))
            caseDefaultLabel = "Case default";

        var summary = ApplicationProfileInstanceAddressResolver.GetInstanceSummaryAddress(application, slot: 1);
        if (summary != null && summary.ID != Guid.Empty && summary.ID != excludeObjectId)
        {
            choices.Add(Choice(
                summary.ID,
                caseDefaultLabel + " — " + AddressSummary(summary),
                AddressFields(summary)));
        }

        var linked = ApplicationProfileInstanceAddressResolver.ResolveAddressSlot(
            application, person, slot: 1, objectSpace);
        if (linked != null && linked.ID != Guid.Empty && linked.ID != excludeObjectId
            && choices.All(c => c.ObjectId != linked.ID))
        {
            choices.Add(Choice(linked.ID, AddressSummary(linked), AddressFields(linked)));
        }

        if (objectSpace != null)
            AppendSharedAddressCatalog(choices, objectSpace, excludeObjectId);
        return choices;
    }

    private static void AppendSharedAddressCatalog(
        List<PersonLinkReviewChoice> choices,
        IObjectSpace objectSpace,
        Guid excludeObjectId)
    {
        var seenIds = new HashSet<Guid>(choices.Select(c => c.ObjectId));
        var seenLabels = new HashSet<string>(
            choices
                .Select(c => PersonSearchTextNormalizer.Fold(
                    (c.Label ?? string.Empty).Trim()))
                .Where(key => key.Length > 0),
            StringComparer.Ordinal);
        var rows = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .AsNoTracking()
            .Include(row => row.Region)
            .Include(row => row.City)
            .Include(row => row.Lodging)
            .Include(row => row.Hotel)
            .Include(row => row.Hospital)
            .Include(row => row.OtherSite)
            .OrderByDescending(row => row.ID)
            .Take(1500)
            .AsEnumerable()
            .ToList();

        foreach (var row in rows)
        {
            if (row.ID == Guid.Empty || row.ID == excludeObjectId || !seenIds.Add(row.ID))
                continue;

            var label = AddressOfResidenceReportText.CityAndStreet(row);
            if (string.IsNullOrWhiteSpace(label))
                label = row.DisplayAddress;
            if (string.IsNullOrWhiteSpace(label))
                label = AddressSummary(row);

            var dedupKey = PersonSearchTextNormalizer.Fold(
                (label ?? string.Empty).Trim());
            if (dedupKey.Length > 0 && !seenLabels.Add(dedupKey))
                continue;

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
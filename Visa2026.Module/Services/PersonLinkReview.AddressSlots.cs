using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
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
            var single = Address1TextNoteLine(objectSpace, application, person, fallbackLabel);
            single.SlotIndex = 0;
            single.SlotCount = 0;
            return new[] { single };
        }

        var slot1 = Address1TextNoteLine(objectSpace, application, person, "Address 1");
        slot1.SlotIndex = 1;
        slot1.SlotCount = maxSlots;

        var slot2 = Address2TextNoteLine(objectSpace, application, person);
        slot2.SlotIndex = 2;
        slot2.SlotCount = maxSlots;
        return new[] { slot1, slot2 };
    }

    internal static PersonLinkReviewLine Address1TextNoteLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        string label)
    {
        string? text = null;
        if (objectSpace != null && application.ID != Guid.Empty && person.ID != Guid.Empty)
            text = CasePersonAddress2Note.GetAddress1Text(objectSpace, application.ID, person.ID);

        if (string.IsNullOrWhiteSpace(text) && application.PersonAddress2Notes != null)
        {
            text = application.PersonAddress2Notes
                .FirstOrDefault(n => n != null && n.PersonId == person.ID)
                ?.Address1Text;
            text = CaseAddressAutocomplete.NormalizeDisplay(text);
        }

        // Legacy dual-read: linked AddressOfResidence catalog row.
        if (string.IsNullOrWhiteSpace(text) && objectSpace != null)
        {
            var resolved = ApplicationProfileInstanceAddressResolver.ResolveAddressSlot(
                application, person, slot: 1, objectSpace);
            text = CaseAddressAutocomplete.NormalizeDisplay(
                AddressOfResidenceReportText.CityAndStreet(resolved));
        }

        if (string.IsNullOrWhiteSpace(text))
            text = CaseAddressAutocomplete.NormalizeDisplay(application.Address1DefaultText);

        var summary = string.IsNullOrWhiteSpace(text) ? "—" : text!;
        return new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            Label = label,
            Summary = summary,
            ObjectId = Guid.Empty,
            UsesMultilineText = true,
            TextValue = text,
            Choices = Array.Empty<PersonLinkReviewChoice>(),
            Fields = Array.Empty<PersonLinkReviewField>(),
        };
    }

    internal static PersonLinkReviewLine Address2TextNoteLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person)
    {
        string? text = null;
        if (objectSpace != null && application.ID != Guid.Empty && person.ID != Guid.Empty)
            text = CasePersonAddress2Note.GetText(objectSpace, application.ID, person.ID);

        if (string.IsNullOrWhiteSpace(text) && application.PersonAddress2Notes != null)
        {
            text = application.PersonAddress2Notes
                .FirstOrDefault(n => n != null && n.PersonId == person.ID)
                ?.Address2Text;
            text = CaseAddressAutocomplete.NormalizeDisplay(text);
        }

        if (string.IsNullOrWhiteSpace(text))
            text = CaseAddressAutocomplete.NormalizeDisplay(application.Address2DefaultText);

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

    /// <summary>
    /// Obsolete: Address slots are free text. Kept as no-op for older call sites.
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
        // Address 1/2 are free-text notes — no catalog preference.
        _ = objectSpace;
        _ = application;
        _ = person;
        _ = addressLines;
        _ = pinned;
    }

    private static void ApplyJoinAddressLinks(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        IEnumerable<PersonLinkReviewLine> addressLines)
    {
        foreach (var line in addressLines.OrderBy(l => l.SlotIndex))
        {
            if (!line.UsesMultilineText)
                continue;

            var slot = line.SlotIndex > 0 ? line.SlotIndex : 1;
            var incoming = CaseAddressAutocomplete.NormalizeDisplay(line.TextValue);

            if (slot >= 2)
            {
                if (incoming == null
                    && CasePersonAddress2Note.GetText(objectSpace, application.ID, person.ID) == null)
                {
                    incoming = CaseAddressAutocomplete.NormalizeDisplay(application.Address2DefaultText);
                }

                CasePersonAddress2Note.Upsert(objectSpace, application, person, incoming);
                CaseSavedAddress.Remember(
                    objectSpace, application, ApplicationProfileSavedAddressField.Address2, incoming);
                continue;
            }

            if (incoming == null
                && CasePersonAddress2Note.GetAddress1Text(objectSpace, application.ID, person.ID) == null)
            {
                incoming = CaseAddressAutocomplete.NormalizeDisplay(application.Address1DefaultText);
            }

            CasePersonAddress2Note.UpsertAddress1(objectSpace, application, person, incoming);
            CaseSavedAddress.Remember(
                objectSpace, application, ApplicationProfileSavedAddressField.Address1, incoming);
        }
    }
}
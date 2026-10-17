using System;
using System.Collections.Generic;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Fields the A. Maglumat preview reads from either work-permit view.</summary>
public interface IRdWorkPermitMaglumatRow
{
    Guid ID { get; }
    Guid? PersonOid { get; }
    string PersonName { get; }
    string ProjectName { get; }
    DateTime? DateOfBirth { get; }
    string CitizenshipCode { get; }
    string PassportNumber { get; }
    DateTime? PassportExpirationDate { get; }
    string EducationLabel { get; }
    string PositionLabel { get; }
    string AddressRegionTm { get; }
    string AddressCityTm { get; }
    string AddressStreet { get; }
    string WorkPermitNumber { get; }
    Guid? WorkPermitOid { get; }
    DateTime? StartDate { get; }
    DateTime? ExpirationDate { get; }
    string WorkPermitBundle { get; }
    Guid? VisaOid { get; }
    string VisaNumber { get; }
    DateTime? VisaStartDate { get; }
    DateTime? VisaEndDate { get; }
    string StatusLabel { get; }
}

/// <summary>One valid work permit inside an A Maglumat person row.</summary>
public sealed class WorkPermitBundleSlot
{
    public Guid HeaderId { get; init; }
    public Guid ItemId { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Start { get; init; } = string.Empty;
    public string End { get; init; } = string.Empty;
    public string Locations { get; init; } = string.Empty;

    /// <summary>Issued additional-location applications for this permit, newest first.</summary>
    public IReadOnlyList<WorkPermitAdditionalApplication> AdditionalApplications { get; init; }
        = Array.Empty<WorkPermitAdditionalApplication>();

    /// <summary>Same applications as display text: number, date, and places, one block per application.</summary>
    public string AdditionalLocations { get; init; } = string.Empty;
}

/// <summary>One issued additional-location application linked to a work permit.</summary>
public sealed class WorkPermitAdditionalApplication
{
    public Guid ApplicationId { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Date { get; init; } = string.Empty;
    public string Locations { get; init; } = string.Empty;
}

/// <summary>
/// WorkPermitBundle column: one permit per line, fields separated by the ASCII record separator.
/// Order is newest start date first.
/// </summary>
public static class WorkPermitBundleText
{
    private const char Field = '\u001e';

    public static IReadOnlyList<WorkPermitBundleSlot> Parse(string? bundle)
    {
        if (string.IsNullOrWhiteSpace(bundle))
            return Array.Empty<WorkPermitBundleSlot>();

        var slots = new List<WorkPermitBundleSlot>();
        foreach (var line in bundle.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(Field);
            if (parts.Length < 5)
                continue;
            var additional = parts.Length > 6
                ? ParseAdditional(parts[6])
                : Array.Empty<WorkPermitAdditionalApplication>();
            slots.Add(new WorkPermitBundleSlot
            {
                HeaderId = Guid.TryParse(parts[0], out var header) ? header : Guid.Empty,
                ItemId = Guid.TryParse(parts[1], out var item) ? item : Guid.Empty,
                Number = parts[2].Trim(),
                Start = parts[3].Trim(),
                End = parts[4].Trim(),
                Locations = parts.Length > 5 ? parts[5].Trim() : string.Empty,
                AdditionalApplications = additional,
                AdditionalLocations = FormatAdditional(additional)
            });
        }

        return slots;
    }

    public static string Display(string? bundle)
    {
        var blocks = new List<string>();
        foreach (var slot in Parse(bundle))
        {
            var lines = new List<string>();
            if (slot.Number.Length > 0)
                lines.Add(slot.Number);
            if (slot.Start.Length > 0)
                lines.Add(slot.Start);
            if (slot.End.Length > 0)
                lines.Add(slot.End);
            if (lines.Count > 0)
                blocks.Add(string.Join("\n", lines));
        }

        return string.Join("\n\n", blocks);
    }

    private static IReadOnlyList<WorkPermitAdditionalApplication> ParseAdditional(string encoded)
    {
        if (string.IsNullOrWhiteSpace(encoded))
            return Array.Empty<WorkPermitAdditionalApplication>();

        var applications = new List<WorkPermitAdditionalApplication>();
        foreach (var record in encoded.Split('\u001d', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = record.Split('\u001f');
            if (parts.Length < 2)
                continue;
            applications.Add(new WorkPermitAdditionalApplication
            {
                ApplicationId = Guid.TryParse(parts[0], out var id) ? id : Guid.Empty,
                Number = parts[1].Trim(),
                Date = parts.Length > 2 ? parts[2].Trim() : string.Empty,
                Locations = parts.Length > 3 ? parts[3].Trim() : string.Empty
            });
        }

        return applications;
    }

    private static string FormatAdditional(IReadOnlyList<WorkPermitAdditionalApplication> applications)
    {
        var blocks = new List<string>();
        foreach (var application in applications)
        {
            var lines = new List<string>();
            if (application.Number.Length > 0)
                lines.Add(application.Number);
            if (application.Date.Length > 0)
                lines.Add(application.Date);
            if (application.Locations.Length > 0)
                lines.Add(application.Locations);
            if (lines.Count > 0)
                blocks.Add(string.Join("\n", lines));
        }

        return string.Join("\n\n", blocks);
    }
}
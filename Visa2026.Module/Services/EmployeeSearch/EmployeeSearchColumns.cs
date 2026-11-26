using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.EmployeeSearch;

/// <summary>
/// Shared result-column catalog. Keys are property names. The first seven match the original result and start included.
/// </summary>
public static class EmployeeSearchColumns
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly EmployeeSearchColumn[] Catalog =
    [
        new("FullName", "Column.Name", true),
        new("Age", "Column.Age", true),
        new("Nationality", "Citizenship", true),
        new("WhyMatched", "Column.Why", true),
        new("Position", "Position", true),
        new("Visa", "Column.Visa", true),
        new("ProjectContract", "Column.Contract", true),
        new("FirstName", "FirstName", false),
        new("MiddleName", "MiddleName", false),
        new("LastName", "LastName", false),
        new("PersonalNumber", "PersonalNumber", false),
        new("DateOfBirth", "DateOfBirth", false),
        new("Gender", "Gender", false),
        new("MaritalStatus", "MaritalStatus", false),
        new("CountryOfBirth", "CountryOfBirth", false),
        new("BirthPlace", "PlaceOfBirth", false),
        new("Email", "Email", false),
        new("HireDate", "HireDate", false),
        new("IsArchived", "Archived", false),
        new("Subcontractor", "Subcontractor", false),
        new("ForeignAddress", "ForeignAddress", false),
        new("ForeignAddressCountry", "ForeignCountry", false),
        new("PreviousWorkplacesInTurkmenistan", "PreviousWork", false),
        new("Department", "Department", false),
        new("ActualPosition", "ActualPosition", false),
        new("VisaNumber", "VisaNumber", false),
        new("VisaType", "VisaType", false),
        new("VisaCategory", "VisaCategory", false),
        new("VisaStartDate", "VisaStart", false),
        new("VisaExpirationDate", "VisaEnd", false),
        new("PassportNumber", "PassportNumber", false),
        new("PassportType", "PassportType", false),
        new("PassportIssuedCountry", "IssuedCountry", false),
        new("PassportExpirationDate", "PassportExpiration", false),
    ];

    public static IReadOnlyList<EmployeeSearchColumnState> Defaults() =>
        Catalog.Select(Describe).ToList();

    public static IReadOnlyList<EmployeeSearchColumnState> Resolve(IObjectSpace objectSpace)
    {
        var defaults = Defaults();
        var known = defaults.ToDictionary(column => column.Key, StringComparer.OrdinalIgnoreCase);
        var ordered = new List<EmployeeSearchColumnState>(defaults.Count);
        var settings = EmployeeSearchColumnSettings.TryGetInstance(objectSpace);

        foreach (var entry in Parse(settings?.ColumnsJson))
        {
            if (!known.TryGetValue(entry.K, out var column))
                continue;
            if (ordered.Any(item => string.Equals(item.Key, column.Key, StringComparison.OrdinalIgnoreCase)))
                continue;

            ordered.Add(new EmployeeSearchColumnState
            {
                Key = column.Key,
                Included = entry.On,
                Caption = column.Caption,
            });
        }

        foreach (var column in defaults)
        {
            if (ordered.Any(item => string.Equals(item.Key, column.Key, StringComparison.OrdinalIgnoreCase)))
                continue;
            ordered.Add(column);
        }

        if (ordered.Count > 0 && !ordered.Any(column => column.Included))
            ordered[0].Included = true;

        return ordered;
    }

    public static void Save(IObjectSpace objectSpace, IReadOnlyList<EmployeeSearchColumnState> columns)
    {
        var settings = EmployeeSearchColumnSettings.GetOrCreateInstance(objectSpace);
        settings.ColumnsJson = JsonSerializer.Serialize(
            columns.Select(column => new StoredColumn
            {
                K = column.Key ?? string.Empty,
                On = column.Included,
            }).ToList(),
            JsonOptions);
        objectSpace.CommitChanges();
    }

    private static EmployeeSearchColumnState Describe(EmployeeSearchColumn column) =>
        new()
        {
            Key = column.Key,
            Included = column.IncludedByDefault,
            Caption = EmployeeSearchText.Get(column.CaptionKey),
        };

    private static List<StoredColumn> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<StoredColumn>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record EmployeeSearchColumn(string Key, string CaptionKey, bool IncludedByDefault);

    private sealed class StoredColumn
    {
        public string K { get; set; } = string.Empty;
        public bool On { get; set; }
    }
}

public sealed class EmployeeSearchColumnState
{
    public string Key { get; set; } = string.Empty;
    public bool Included { get; set; }
    public string Caption { get; set; } = string.Empty;
}
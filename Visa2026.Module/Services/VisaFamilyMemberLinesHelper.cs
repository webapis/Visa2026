using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Parse and format <see cref="Person.VisaApplicationFamilyMembersText"/> lines
/// ({FullName}; {dd.MM.yyyy}; {Relationship.NameTm}; {Country.Code}).
/// </summary>
public static class VisaFamilyMemberLinesHelper
{
    public const string DateFormat = "dd.MM.yyyy";

    private static readonly string[] NewLineSeparators = ["\r\n", "\n"];

    public static IReadOnlyList<VisaFamilyMemberLineDto> Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<VisaFamilyMemberLineDto>();
        }

        var lines = text.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new List<VisaFamilyMemberLineDto>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            result.Add(ParseLine(line));
        }

        return result;
    }

    public static string? Format(IEnumerable<VisaFamilyMemberLineDto>? lines)
    {
        if (lines == null)
        {
            return null;
        }

        var formatted = lines
            .Select(FormatStorageLine)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return formatted.Count == 0 ? null : string.Join(Environment.NewLine, formatted);
    }

    /// <summary>Visa PDF block: three fields per line (no country code).</summary>
    public static string? FormatForVisaPdfAggregate(string? text)
    {
        var lines = Parse(text);
        if (lines.Count == 0)
        {
            return null;
        }

        var formatted = lines
            .Select(FormatVisaPdfLine)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return formatted.Count == 0 ? null : string.Join(Environment.NewLine, formatted);
    }

    /// <summary>Şahsy kagyz Maşgala ýagdaýy (comma-separated segments).</summary>
    public static string? FormatSahsyKagyzFamilyStatus(string? text)
    {
        var lines = Parse(text);
        if (lines.Count == 0)
        {
            return null;
        }

        var segments = new List<string>();
        foreach (var row in lines)
        {
            var segment = FormatSahsyKagyzSegment(row);
            if (!string.IsNullOrWhiteSpace(segment))
            {
                segments.Add(segment);
            }
        }

        return segments.Count == 0 ? null : string.Join(", ", segments);
    }

    public static string FormatDisplaySummary(string? text, string emptyMessage, string memberCountFormat)
    {
        var rows = Parse(text);
        if (rows.Count == 0)
        {
            return emptyMessage;
        }

        var first = FormatLineForSummary(rows[0]);
        if (rows.Count == 1)
        {
            return first;
        }

        return string.Format(memberCountFormat, rows.Count) + " — " + first;
    }

    public static bool TryValidate(
        IReadOnlyList<VisaFamilyMemberLineDto> lines,
        out string? errorMessage)
    {
        errorMessage = null;
        if (lines == null || lines.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var row = lines[i];
            var lineNumber = i + 1;
            if (string.IsNullOrWhiteSpace(row.FullName))
            {
                errorMessage = $"Line {lineNumber}: full name is required.";
                return false;
            }

            if (!row.BirthDate.HasValue)
            {
                errorMessage = $"Line {lineNumber}: birth date is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(row.RelationshipNameTm))
            {
                errorMessage = $"Line {lineNumber}: relationship is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(row.CountryCode))
            {
                errorMessage = $"Line {lineNumber}: country of residence is required.";
                return false;
            }
        }

        return true;
    }

    public static IReadOnlyList<RelationshipLookupItem> LoadRelationshipOptions(IObjectSpace? objectSpace)
    {
        if (objectSpace == null)
        {
            return Array.Empty<RelationshipLookupItem>();
        }

        return objectSpace.GetObjects<Relationship>()
            .Where(r => r != null)
            .OrderBy(r => r.NameTm ?? r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RelationshipLookupItem
            {
                Oid = r.ID,
                NameTm = (r.NameTm ?? r.Name ?? string.Empty).Trim(),
            })
            .Where(r => !string.IsNullOrEmpty(r.NameTm))
            .ToList();
    }

    public static IReadOnlyList<CountryLookupItem> LoadCountryOptions(IObjectSpace? objectSpace)
    {
        if (objectSpace == null)
        {
            return Array.Empty<CountryLookupItem>();
        }

        return objectSpace.GetObjects<Country>()
            .Where(c => c != null)
            .OrderBy(c => c.NameTm ?? c.Name ?? c.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(c => new CountryLookupItem
            {
                Oid = c.ID,
                Code = (c.Code ?? string.Empty).Trim(),
                NameTm = (c.NameTm ?? c.Name ?? string.Empty).Trim(),
            })
            .Where(c => !string.IsNullOrEmpty(c.Code))
            .ToList();
    }

    public static IReadOnlyList<RelationshipLookupItem> MergeRelationshipOptionsForRow(
        IReadOnlyList<RelationshipLookupItem> baseOptions,
        VisaFamilyMemberLineDto? row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.RelationshipNameTm))
        {
            return baseOptions;
        }

        var nameTm = row.RelationshipNameTm.Trim();
        if (baseOptions.Any(o => string.Equals(o.NameTm, nameTm, StringComparison.OrdinalIgnoreCase)))
        {
            return baseOptions;
        }

        var merged = baseOptions.ToList();
        merged.Add(new RelationshipLookupItem
        {
            Oid = row.RelationshipOid ?? Guid.Empty,
            NameTm = nameTm,
        });
        return merged;
    }

    public static IReadOnlyList<CountryLookupItem> MergeCountryOptionsForRow(
        IReadOnlyList<CountryLookupItem> baseOptions,
        VisaFamilyMemberLineDto? row)
    {
        if (row == null || string.IsNullOrWhiteSpace(row.CountryCode))
        {
            return baseOptions;
        }

        var code = row.CountryCode.Trim();
        if (baseOptions.Any(o => string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            return baseOptions;
        }

        var merged = baseOptions.ToList();
        merged.Add(new CountryLookupItem
        {
            Oid = row.CountryOid ?? Guid.Empty,
            Code = code,
            NameTm = code,
        });
        return merged;
    }

    public static Relationship? ResolveRelationship(IObjectSpace? objectSpace, Guid? relationshipOid, string? relationshipNameTm)
    {
        if (objectSpace == null)
        {
            return null;
        }

        if (relationshipOid is Guid oid && oid != Guid.Empty)
        {
            return objectSpace.GetObjectByKey<Relationship>(oid);
        }

        if (string.IsNullOrWhiteSpace(relationshipNameTm))
        {
            return null;
        }

        var name = relationshipNameTm.Trim();
        return objectSpace.GetObjects<Relationship>()
            .FirstOrDefault(r =>
                string.Equals(r.NameTm?.Trim(), name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
    }

    public static Country? ResolveCountry(IObjectSpace? objectSpace, Guid? countryOid, string? countryCode)
    {
        if (objectSpace == null)
        {
            return null;
        }

        if (countryOid is Guid oid && oid != Guid.Empty)
        {
            return objectSpace.GetObjectByKey<Country>(oid);
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var code = countryCode.Trim();
        return objectSpace.GetObjects<Country>()
            .FirstOrDefault(c => string.Equals(c.Code?.Trim(), code, StringComparison.OrdinalIgnoreCase));
    }

    public static void ApplyRelationshipSelection(VisaFamilyMemberLineDto row, Relationship? relationship)
    {
        if (row == null)
        {
            return;
        }

        if (relationship == null)
        {
            row.RelationshipOid = null;
            return;
        }

        row.RelationshipOid = relationship.ID;
        row.RelationshipNameTm = (relationship.NameTm ?? relationship.Name ?? string.Empty).Trim();
    }

    public static void ApplyCountrySelection(VisaFamilyMemberLineDto row, Country? country)
    {
        if (row == null)
        {
            return;
        }

        if (country == null)
        {
            row.CountryOid = null;
            return;
        }

        row.CountryOid = country.ID;
        row.CountryCode = (country.Code ?? string.Empty).Trim();
    }

    private static VisaFamilyMemberLineDto ParseLine(string line)
    {
        var parts = line.Split(';');
        var dto = new VisaFamilyMemberLineDto();

        if (parts.Length == 0)
        {
            dto.FullName = line.Trim();
            dto.IsLegacyIncomplete = true;
            return dto;
        }

        dto.FullName = parts[0].Trim();

        if (parts.Length == 1)
        {
            dto.IsLegacyIncomplete = string.IsNullOrWhiteSpace(dto.FullName);
            return dto;
        }

        var datePart = parts[1].Trim();
        if (TryParseDate(datePart, out var birthDate))
        {
            dto.BirthDate = birthDate;
        }

        if (parts.Length >= 3)
        {
            dto.RelationshipNameTm = parts[2].Trim();
        }

        if (parts.Length >= 4)
        {
            dto.CountryCode = parts[3].Trim();
        }

        dto.IsLegacyIncomplete =
            string.IsNullOrWhiteSpace(dto.FullName)
            || !dto.BirthDate.HasValue
            || string.IsNullOrWhiteSpace(dto.RelationshipNameTm)
            || string.IsNullOrWhiteSpace(dto.CountryCode);

        return dto;
    }

    private static string FormatStorageLine(VisaFamilyMemberLineDto row)
    {
        if (row == null)
        {
            return string.Empty;
        }

        var name = row.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name) || !row.BirthDate.HasValue)
        {
            return string.Empty;
        }

        var rel = row.RelationshipNameTm?.Trim() ?? string.Empty;
        var code = row.CountryCode?.Trim() ?? string.Empty;
        return $"{name}; {row.BirthDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}; {rel}; {code}";
    }

    private static string FormatVisaPdfLine(VisaFamilyMemberLineDto row)
    {
        if (row == null)
        {
            return string.Empty;
        }

        var name = row.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name) || !row.BirthDate.HasValue)
        {
            return string.Empty;
        }

        var rel = row.RelationshipNameTm?.Trim() ?? string.Empty;
        return $"{name}; {row.BirthDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}; {rel}";
    }

    private static string? FormatSahsyKagyzSegment(VisaFamilyMemberLineDto row)
    {
        if (row == null
            || string.IsNullOrWhiteSpace(row.FullName)
            || !row.BirthDate.HasValue
            || string.IsNullOrWhiteSpace(row.RelationshipNameTm)
            || string.IsNullOrWhiteSpace(row.CountryCode))
        {
            return null;
        }

        var relLower = row.RelationshipNameTm.Trim().ToLowerInvariant();
        var code = row.CountryCode.Trim();
        return $"{relLower}-{row.FullName.Trim()} {row.BirthDate.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}ý. {code}.";
    }

    private static string FormatLineForSummary(VisaFamilyMemberLineDto row)
    {
        var basePart = string.IsNullOrWhiteSpace(row.RelationshipNameTm)
            ? $"{row.FullName?.Trim()}; {row.BirthDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "??"}"
            : $"{row.FullName?.Trim()}; {row.BirthDate?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? "??"}; {row.RelationshipNameTm.Trim()}";

        return string.IsNullOrWhiteSpace(row.CountryCode)
            ? basePart
            : basePart + "; " + row.CountryCode.Trim();
    }

    private static bool TryParseDate(string value, out DateTime date)
    {
        if (DateTime.TryParseExact(
                value,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out date))
        {
            return true;
        }

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

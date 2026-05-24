using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

/// <summary>
/// Excel import hooks for organization singletons and related <see cref="SystemSettings"/> fields.
/// </summary>
public static class OrganizationImportHooks
{
    public static async Task ApplyCompanyProfileSettingsAsync(
        Guid companyProfileId,
        List<object> row,
        Dictionary<string, int> headerIndex,
        ApiClient api)
    {
        _ = companyProfileId;

        var settingsPayload = new Dictionary<string, object?>();
        TryAddScalar(settingsPayload, row, headerIndex, "AppNumberPrefix");
        TryAddScalar(settingsPayload, row, headerIndex, "AppNumberFormat");
        TryAddInt(settingsPayload, row, headerIndex, "ApplicationNumberPadding");
        TryAddInt(settingsPayload, row, headerIndex, "ApplicationNumberSeed");

        if (settingsPayload.Count == 0)
            return;

        var settings = (await api.QueryAsync<SystemSettingsDto>("SystemSettings", "$top=1")).FirstOrDefault();
        if (settings == null)
        {
            Console.WriteLine("  ⚠ SystemSettings row not found — app numbering columns on Company sheet were skipped.");
            return;
        }

        await api.UpdateAsync("SystemSettings", settings.Id, settingsPayload);
        Console.WriteLine("  ✓ Updated SystemSettings application numbering from Company sheet.");
    }

    private static void TryAddScalar(
        IDictionary<string, object?> payload,
        List<object> row,
        Dictionary<string, int> headerIndex,
        string header)
    {
        if (!headerIndex.TryGetValue(header, out int colIdx))
            return;

        var raw = colIdx < row.Count ? row[colIdx]?.ToString()?.Trim() : "";
        if (!string.IsNullOrWhiteSpace(raw))
            payload[header] = raw;
    }

    private static void TryAddInt(
        IDictionary<string, object?> payload,
        List<object> row,
        Dictionary<string, int> headerIndex,
        string header)
    {
        if (!headerIndex.TryGetValue(header, out int colIdx))
            return;

        var raw = colIdx < row.Count ? row[colIdx]?.ToString()?.Trim() : "";
        if (int.TryParse(raw, out var value))
            payload[header] = value;
    }
}

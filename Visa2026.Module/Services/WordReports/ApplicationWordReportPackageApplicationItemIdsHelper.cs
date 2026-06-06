using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Visa2026.Module.Services.WordReports;

/// <summary>Serializes selected application line ids for item-scoped Resminamalar batches.</summary>
public static class ApplicationWordReportPackageApplicationItemIdsHelper
{
    public static string? Serialize(IReadOnlyList<Guid>? applicationItemIds)
    {
        if (applicationItemIds == null || applicationItemIds.Count == 0)
            return null;

        var normalized = applicationItemIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    public static IReadOnlyList<Guid>? Deserialize(string? selectedApplicationItemIdsJson)
    {
        if (string.IsNullOrWhiteSpace(selectedApplicationItemIdsJson))
            return null;

        try
        {
            var ids = JsonSerializer.Deserialize<List<Guid>>(selectedApplicationItemIdsJson)?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            return ids is { Count: > 0 } ? ids : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

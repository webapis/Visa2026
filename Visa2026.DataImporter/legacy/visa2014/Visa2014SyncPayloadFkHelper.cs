namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SyncPayloadFkHelper
{
    internal static bool TryGetPayloadFkId(IReadOnlyDictionary<string, object?> payload, string key, out Guid id)
    {
        id = default;
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is Guid guid)
        {
            id = guid;
            return true;
        }

        var idProperty = raw.GetType().GetProperty("ID");
        if (idProperty?.GetValue(raw) is Guid nested)
        {
            id = nested;
            return true;
        }

        return false;
    }

    internal static bool TryGetPayloadString(IReadOnlyDictionary<string, object?> payload, string key, out string value)
    {
        value = "";
        if (!payload.TryGetValue(key, out var raw) || raw is not string text)
            return false;

        value = text.Trim();
        return value.Length > 0;
    }
}

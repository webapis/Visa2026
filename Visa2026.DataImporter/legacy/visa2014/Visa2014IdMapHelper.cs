using System.Text.Json;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014IdMapHelper
{
    public static Dictionary<Guid, Guid> Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Id-map not found: {path}", path);

        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidOperationException($"Id-map is empty or invalid: {path}");

        var map = new Dictionary<Guid, Guid>();
        foreach (var (legacyText, targetText) in raw)
        {
            if (!Guid.TryParse(legacyText, out var legacyOid))
                continue;
            if (!Guid.TryParse(targetText, out var targetId))
                continue;
            map[legacyOid] = targetId;
        }

        return map;
    }
}

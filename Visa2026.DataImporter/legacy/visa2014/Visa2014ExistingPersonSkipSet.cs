using System.Text;
using System.Text.Json;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Legacy person ids already on the target when a catch-up starts.
/// Child rows and files for these people are not imported. People posted in the
/// same run stay out of the set, so their passport, education, position,
/// address, salary, photo, and scans still import.
/// </summary>
internal static class Visa2014ExistingPersonSkipSet
{
    public const string Flag = "--skip-children-of-existing-persons";
    public const string FileName = "Person.skip-children.json";

    public static bool Requested(IReadOnlyList<string> args) =>
        args.Any(a => string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase));

    public static string PathBesidePersonIdMap(string personIdMapPath)
    {
        var full = Path.GetFullPath(personIdMapPath);
        var directory = Path.GetDirectoryName(full);
        if (string.IsNullOrEmpty(directory))
            directory = Directory.GetCurrentDirectory();
        return Path.Combine(directory, FileName);
    }

    /// <summary>
    /// Legacy ids whose Visa2026 person was not created in this run.
    /// Aliases that point at a newly posted person stay out of the set.
    /// </summary>
    public static HashSet<Guid> SelectExisting(
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlySet<Guid> postedTargetIds)
    {
        var existing = new HashSet<Guid>();
        foreach (var (legacyOid, targetId) in personIdMap)
        {
            if (!postedTargetIds.Contains(targetId))
                existing.Add(legacyOid);
        }

        return existing;
    }

    public static bool ShouldSkip(IReadOnlySet<Guid>? existingLegacyPersonOids, Guid legacyPersonOid) =>
        existingLegacyPersonOids != null
        && legacyPersonOid != Guid.Empty
        && existingLegacyPersonOids.Contains(legacyPersonOid);

    public static async Task SaveAsync(string path, IReadOnlyCollection<Guid> legacyPersonOids)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var ids = legacyPersonOids
            .Select(id => id.ToString("D"))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        var json = JsonSerializer.Serialize(ids, new JsonSerializerOptions { WriteIndented = true });
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        await File.WriteAllTextAsync(path, json, utf8);
    }

    public static HashSet<Guid> Load(string path)
    {
        var ids = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? [];
        var set = new HashSet<Guid>();
        foreach (var text in ids)
        {
            if (Guid.TryParse(text, out var id))
                set.Add(id);
        }

        return set;
    }

    public static ExistingPersonSkipLoad LoadForChildRun(IReadOnlyList<string> args, string personIdMapPath)
    {
        if (!Requested(args))
            return new ExistingPersonSkipLoad(false, null, null);

        var path = ReadOption(args, "--existing-person-skip-set")
            ?? PathBesidePersonIdMap(personIdMapPath);
        if (!File.Exists(path))
        {
            return new ExistingPersonSkipLoad(
                true,
                null,
                "ERR Person skip-children set was not found: " + path
                + ". Run --entity Person with --skip-children-of-existing-persons first.");
        }

        var set = Load(path);
        Console.WriteLine($"INF Skip-by-person set: {set.Count} legacy people ({path})");
        return new ExistingPersonSkipLoad(true, set, null);
    }

    private static string? ReadOption(IReadOnlyList<string> args, string optionName)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (!string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (i + 1 < args.Count && !args[i + 1].StartsWith('-'))
                return args[i + 1];
        }

        return null;
    }
}

internal readonly record struct ExistingPersonSkipLoad(
    bool Requested,
    IReadOnlySet<Guid>? Set,
    string? Error);

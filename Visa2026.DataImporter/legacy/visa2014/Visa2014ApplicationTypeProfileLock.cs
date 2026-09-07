using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014ApplicationTypeProfileLockFile
{
    public string? Version { get; set; }
    public string? Status { get; set; }
    public List<Visa2014ApplicationTypeProfileLockRow>? Rows { get; set; }
}

internal sealed class Visa2014ApplicationTypeProfileLockRow
{
    public List<string>? SourceComposites { get; set; }
    public string? SourceLabel { get; set; }
    public string TargetApplicationTypeName { get; set; } = "";
    public string ApplicationProfileCode { get; set; } = "";
    public string? ApplicationProfileName { get; set; }
}

/// <summary>
/// Locked VISA2014 composite / Visa2026 ApplicationType.Name / ApplicationProfile template.
/// Import must not pick a profile from shared <c>ApplicationType.Code</c>.
/// </summary>
internal static class Visa2014ApplicationTypeProfileLock
{
    private static readonly object Gate = new();
    private static Visa2014ApplicationTypeProfileLockIndex? Cached;

    public static string? ResolveProfileCode(string? targetApplicationTypeName)
    {
        if (string.IsNullOrWhiteSpace(targetApplicationTypeName))
            return null;

        var index = Load();
        return index.ByTargetTypeName.TryGetValue(targetApplicationTypeName.Trim(), out var row)
            ? row.ApplicationProfileCode
            : null;
    }

    public static bool TryGetByTargetTypeName(string? targetApplicationTypeName, out Visa2014ApplicationTypeProfileLockRow row)
    {
        row = null!;
        if (string.IsNullOrWhiteSpace(targetApplicationTypeName))
            return false;

        return Load().ByTargetTypeName.TryGetValue(targetApplicationTypeName.Trim(), out row!);
    }

    public static bool TryGetBySourceComposite(string? sourceComposite, out Visa2014ApplicationTypeProfileLockRow row)
    {
        row = null!;
        if (string.IsNullOrWhiteSpace(sourceComposite))
            return false;

        return Load().BySourceComposite.TryGetValue(sourceComposite.Trim(), out row!);
    }

    public static Visa2014ApplicationTypeProfileLockIndex Load()
    {
        lock (Gate)
        {
            if (Cached != null)
                return Cached;

            var dataImporterRoot = Visa2014ContentRoot.FindDataImporterRoot()
                ?? throw new InvalidOperationException("Could not locate Visa2026.DataImporter content root for application-type-profile-lock.yaml.");
            var path = Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), "application-type-profile-lock.yaml");
            Cached = LoadFromPath(path);
            return Cached;
        }
    }

    internal static Visa2014ApplicationTypeProfileLockIndex LoadFromPath(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("application-type-profile-lock.yaml not found.", path);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var file = deserializer.Deserialize<Visa2014ApplicationTypeProfileLockFile>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("application-type-profile-lock.yaml deserialized to null.");

        if (!string.Equals(file.Status, "approved", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("application-type-profile-lock.yaml status must be approved.");

        var byTarget = new Dictionary<string, Visa2014ApplicationTypeProfileLockRow>(StringComparer.OrdinalIgnoreCase);
        var byComposite = new Dictionary<string, Visa2014ApplicationTypeProfileLockRow>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in file.Rows ?? [])
        {
            if (string.IsNullOrWhiteSpace(row.TargetApplicationTypeName) || string.IsNullOrWhiteSpace(row.ApplicationProfileCode))
                throw new InvalidOperationException("application-type-profile-lock.yaml row is missing targetApplicationTypeName or applicationProfileCode.");

            row.TargetApplicationTypeName = row.TargetApplicationTypeName.Trim();
            row.ApplicationProfileCode = row.ApplicationProfileCode.Trim();

            if (!byTarget.TryAdd(row.TargetApplicationTypeName, row))
            {
                throw new InvalidOperationException(
                    $"application-type-profile-lock.yaml duplicate targetApplicationTypeName '{row.TargetApplicationTypeName}'.");
            }

            foreach (var composite in row.SourceComposites ?? [])
            {
                if (string.IsNullOrWhiteSpace(composite))
                    continue;

                var key = composite.Trim();
                if (!byComposite.TryAdd(key, row))
                {
                    throw new InvalidOperationException(
                        $"application-type-profile-lock.yaml duplicate sourceComposite '{key}'.");
                }
            }
        }

        return new Visa2014ApplicationTypeProfileLockIndex(byTarget, byComposite);
    }
}

internal sealed class Visa2014ApplicationTypeProfileLockIndex
{
    public Visa2014ApplicationTypeProfileLockIndex(
        IReadOnlyDictionary<string, Visa2014ApplicationTypeProfileLockRow> byTargetTypeName,
        IReadOnlyDictionary<string, Visa2014ApplicationTypeProfileLockRow> bySourceComposite)
    {
        ByTargetTypeName = byTargetTypeName;
        BySourceComposite = bySourceComposite;
    }

    public IReadOnlyDictionary<string, Visa2014ApplicationTypeProfileLockRow> ByTargetTypeName { get; }
    public IReadOnlyDictionary<string, Visa2014ApplicationTypeProfileLockRow> BySourceComposite { get; }
}
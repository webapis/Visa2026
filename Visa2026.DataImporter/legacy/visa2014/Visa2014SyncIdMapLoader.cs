namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014SyncIdMapLoader
{
    public static Dictionary<Guid, Guid> LoadOptional(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<Guid, Guid>();

        return Visa2014IdMapHelper.Load(path);
    }

    public static Dictionary<Guid, Guid> LoadRequired(string path) =>
        Visa2014IdMapHelper.Load(path);

    public static Visa2014SyncContext CreateContext(
        string idMapOutputPath,
        Visa2014SyncRowFilter rowFilter,
        bool propagateSoftDeletes = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(idMapOutputPath))!);
        return new Visa2014SyncContext
        {
            RowFilter = rowFilter,
            IdMap = LoadOptional(idMapOutputPath),
            IdMapOutputPath = idMapOutputPath,
            PropagateSoftDeletes = propagateSoftDeletes,
        };
    }
}

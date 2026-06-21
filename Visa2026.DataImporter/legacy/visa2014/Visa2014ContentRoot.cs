namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ContentRoot
{
    public static string? FindDataImporterRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Visa2026.DataImporter");
            if (Directory.Exists(candidate))
                return candidate;

            if (File.Exists(Path.Combine(dir.FullName, "Visa2026.DataImporter.csproj")))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }

    public static string? FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Visa2026.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    public static string LegacyRoot(string dataImporterRoot) =>
        Path.Combine(dataImporterRoot, "legacy", "visa2014");

    public static string FieldMapPath(string dataImporterRoot, string entity) =>
        Path.Combine(LegacyRoot(dataImporterRoot), "field-maps", $"{entity}.yaml");

    public static string? LookupTranslationsPath(string? solutionRoot) =>
        solutionRoot == null
            ? null
            : Path.Combine(solutionRoot, "docs", "VISA2014_MIGRATION", "lookup-translations.yaml");

    public static string DefaultPreviewOutputPath(string dataImporterRoot, string entity) =>
        Path.Combine(LegacyRoot(dataImporterRoot), "preview-export", $"{entity}-preview.xlsx");

    public static string ResolveConnectionString(string? overrideConnection)
    {
        if (!string.IsNullOrWhiteSpace(overrideConnection))
            return overrideConnection.Trim();

        var fromEnv = Environment.GetEnvironmentVariable("VISA2014_SQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        return "Server=localhost\\SQLEXPRESS;Database=VISA2015;Trusted_Connection=True;TrustServerCertificate=True";
    }
}

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

    public static string ResolveConnectionString(string? overrideConnection, string? sourceDefaultConnection = null)
    {
        string cs;
        if (!string.IsNullOrWhiteSpace(overrideConnection))
            cs = overrideConnection.Trim();
        else if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VISA2014_SQL_CONNECTION")))
            cs = Environment.GetEnvironmentVariable("VISA2014_SQL_CONNECTION")!.Trim();
        else if (!string.IsNullOrWhiteSpace(sourceDefaultConnection))
            cs = sourceDefaultConnection.Trim();
        else
            cs = "Server=localhost\\SQLEXPRESS;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True;MultipleActiveResultSets=true";

        return ApplySqlPasswordFromEnvironment(cs);
    }

    /// <summary>
    /// When connection uses User Id without Password, inject from VISA2014_SQL_PASSWORD (OS env).
    /// </summary>
    internal static string ApplySqlPasswordFromEnvironment(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        if (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        if (!connectionString.Contains("User Id=", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.Contains("UserID=", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        var password = Environment.GetEnvironmentVariable("VISA2014_SQL_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
            return connectionString;

        return connectionString.TrimEnd(';') + ";Password=" + password.Trim();
    }
}

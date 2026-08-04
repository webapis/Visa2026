using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using UserManualManifestGenerator.Models;

namespace UserManualManifestGenerator;

internal sealed class BoCatalogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public void Write(string outputDirectory, BoCatalogDocument catalog, NavigationTreeDocument navigationTree, string? guidesRoot)
    {
        Directory.CreateDirectory(outputDirectory);

        var catalogPath = Path.Combine(outputDirectory, "bo-catalog.json");
        var navigationPath = Path.Combine(outputDirectory, "navigation-tree.json");
        File.WriteAllText(catalogPath, JsonSerializer.Serialize(catalog, JsonOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(navigationPath, JsonSerializer.Serialize(navigationTree, JsonOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        if (!string.IsNullOrWhiteSpace(guidesRoot))
            WriteReferencePages(outputDirectory, catalog, guidesRoot);

        Console.WriteLine($"Wrote {catalog.Types.Count} type(s) to {catalogPath}");
        Console.WriteLine($"Wrote navigation tree to {navigationPath}");
    }

    private static void WriteReferencePages(string outputDirectory, BoCatalogDocument catalog, string guidesRoot)
    {
        var locales = new[] { "en", "tr", "tk", "ru" };
        foreach (var locale in locales)
        {
            var localeDocs = Path.Combine(guidesRoot, locale);
            if (!Directory.Exists(localeDocs))
                continue;

            var referenceDir = Path.Combine(outputDirectory, "reference", locale);
            Directory.CreateDirectory(referenceDir);

            var builder = new StringBuilder();
            builder.AppendLine("---");
            builder.AppendLine("title: Business objects");
            builder.AppendLine($"locale: {locale}");
            builder.AppendLine("status: generated");
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine("# Business objects");
            builder.AppendLine();
            builder.AppendLine("This page is generated from the application catalog. Field labels match the on-screen captions in Visa2026.");
            builder.AppendLine();

            foreach (var type in catalog.Types.OrderBy(t => t.UserDocCategory, StringComparer.Ordinal).ThenBy(t => t.DisplayName, StringComparer.Ordinal))
            {
                builder.AppendLine($"## {type.DisplayName}");
                if (!string.IsNullOrWhiteSpace(type.UserDocCategory))
                    builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(type.UserDocCategory))
                    builder.AppendLine($"Category: {type.UserDocCategory}");
                if (!string.IsNullOrWhiteSpace(type.NavigationPath))
                    builder.AppendLine($"Menu path: {type.NavigationPath}");
                builder.AppendLine();

                if (type.Properties.Count == 0)
                {
                    builder.AppendLine("_No officer-visible fields cataloged yet._");
                    builder.AppendLine();
                    continue;
                }

                builder.AppendLine("| Field | Required | Hidden when |");
                builder.AppendLine("|-------|----------|-------------|");
                foreach (var property in type.Properties)
                {
                    var required = property.Required ? "Yes" : "No";
                    var hiddenWhen = string.IsNullOrWhiteSpace(property.HiddenWhen) ? "" : property.HiddenWhen.Replace('|', '/');
                    builder.AppendLine($"| {property.DisplayName} | {required} | {hiddenWhen} |");
                }

                builder.AppendLine();
            }

            var outputPath = Path.Combine(referenceDir, "business-objects.md");
            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}

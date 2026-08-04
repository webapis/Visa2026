using System.Text.Json;
using UserManualManifestGenerator;
using UserManualManifestGenerator.Models;
using Xunit;

namespace UserManualManifestGenerator.Tests;

[Trait("Category", "UserManualDocs")]
public class BoCatalogGeneratorTests
{
    [Fact]
    public void Generated_catalog_includes_pilot_types_with_user_doc_slugs()
    {
        var output = Path.Combine(Path.GetTempPath(), "visa2026-manual-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);

        try
        {
            var generator = new BoCatalogGenerator();
            var catalog = generator.Generate(null, output, guidesRoot: null);

            Assert.Contains(catalog.Types, t => t.Name == "Person" && t.UserDocSlug == "person/overview");
            Assert.Contains(catalog.Types, t => t.Name == "Application" && t.UserDocSlug == "applications/overview");
            Assert.Contains(catalog.Types, t => t.Name == "ApplicationItem" && t.UserDocSlug == "applications/item-overview");
            Assert.Contains(catalog.Types, t => t.Name == "ApplicationProgress" && t.UserDocSlug == "applications/progress");

            var person = catalog.Types.Single(t => t.Name == "Person");
            Assert.Contains(person.Properties, p => p.Name == "FirstName" && p.DisplayName == "First Name");
            Assert.Contains(person.Properties, p => p.Name == "LastName" && p.Required);

            Assert.True(File.Exists(Path.Combine(output, "bo-catalog.json")));
            Assert.True(File.Exists(Path.Combine(output, "navigation-tree.json")));
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Bo_catalog_json_has_required_top_level_fields()
    {
        var output = Path.Combine(Path.GetTempPath(), "visa2026-manual-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);

        try
        {
            var generator = new BoCatalogGenerator();
            generator.Generate(null, output, guidesRoot: null);

            using var stream = File.OpenRead(Path.Combine(output, "bo-catalog.json"));
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;

            Assert.True(root.TryGetProperty("generatedAt", out _));
            Assert.True(root.TryGetProperty("assemblyVersion", out _));
            Assert.True(root.TryGetProperty("types", out var types));
            Assert.Equal(JsonValueKind.Array, types.ValueKind);
            Assert.True(types.GetArrayLength() >= 4);
        }
        finally
        {
            if (Directory.Exists(output))
                Directory.Delete(output, recursive: true);
        }
    }
}

[Trait("Category", "UserManualDocs")]
public class GuideManifestParityTests
{
    [Fact]
    public void English_guide_slugs_are_unique_when_present()
    {
        var repoRoot = BoCatalogGenerator.FindRepoRoot();
        var guidesRoot = Path.Combine(repoRoot, "user-manual", "docs");
        var guides = GuideFrontmatterScanner.Scan(guidesRoot)
            .Where(g => string.Equals(g.Locale, "en", StringComparison.OrdinalIgnoreCase))
            .Where(g => !string.IsNullOrWhiteSpace(g.Slug))
            .ToArray();

        var duplicates = guides
            .GroupBy(g => g.Slug!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        Assert.Empty(duplicates);
    }
}

using UserManualManifestGenerator;
using UserManualManifestGenerator.Models;
using Xunit;

namespace UserManualManifestGenerator.Tests;

[Trait("Category", "UserManualDocs")]
public class GuideFrontmatterScannerTests
{
    [Fact]
    public void Scan_MissingRoot_ReturnsEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "visa2026-guides-missing-" + Guid.NewGuid().ToString("N"));
        Assert.Empty(GuideFrontmatterScanner.Scan(missing));
    }

    [Fact]
    public void Scan_SkipsUnderscoreFiles_AndParsesFrontmatter()
    {
        var root = Path.Combine(Path.GetTempPath(), "visa2026-guides-" + Guid.NewGuid().ToString("N"));
        var guides = Path.Combine(root, "en", "guides", "employee");
        Directory.CreateDirectory(guides);

        try
        {
            File.WriteAllText(
                Path.Combine(guides, "add-visa.md"),
                """
                ---
                slug: employee/add-visa
                bo: Visa
                guideStatus: draft
                ---
                # Add visa
                """);

            File.WriteAllText(
                Path.Combine(guides, "_partial.md"),
                """
                ---
                slug: employee/hidden
                bo: Visa
                ---
                """);

            File.WriteAllText(
                Path.Combine(guides, "no-frontmatter.md"),
                "# No yaml\n");

            var scanned = GuideFrontmatterScanner.Scan(root);

            Assert.Single(scanned);
            Assert.Equal("en", scanned[0].Locale);
            Assert.Equal("employee/add-visa", scanned[0].Slug);
            Assert.Equal("Visa", scanned[0].Bo);
            Assert.Equal("draft", scanned[0].Status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void BuildGuideSlugsByBo_GroupsDistinctSortedSlugs()
    {
        var map = GuideFrontmatterScanner.BuildGuideSlugsByBo(
        [
            new GuideFrontmatter { FilePath = "a.md", Locale = "en", Bo = "Visa", Slug = "employee/add-visa" },
            new GuideFrontmatter { FilePath = "b.md", Locale = "en", Bo = "Visa", Slug = "employee/extend-visa" },
            new GuideFrontmatter { FilePath = "c.md", Locale = "en", Bo = "Visa", Slug = "employee/add-visa" },
            new GuideFrontmatter { FilePath = "d.md", Locale = "en", Bo = "Person", Slug = "person/overview" },
            new GuideFrontmatter { FilePath = "e.md", Locale = "en", Bo = " ", Slug = "ignored" },
            new GuideFrontmatter { FilePath = "f.md", Locale = "en", Bo = "Application", Slug = null },
        ]);

        Assert.Equal(["employee/add-visa", "employee/extend-visa"], map["Visa"]);
        Assert.Equal(["person/overview"], map["Person"]);
        Assert.False(map.ContainsKey("Application"));
    }
}

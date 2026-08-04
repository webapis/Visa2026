using System.Text;
using UserManualManifestGenerator.Models;

namespace UserManualManifestGenerator;

public sealed class BoCatalogGenerator
{
    private readonly ReflectionCatalogReader _reader = new();
    private readonly BoCatalogWriter _writer = new();

    public BoCatalogDocument Generate(string? moduleAssemblyPath, string outputDirectory, string? guidesRoot)
    {
        var guides = string.IsNullOrWhiteSpace(guidesRoot)
            ? Array.Empty<GuideFrontmatter>()
            : GuideFrontmatterScanner.Scan(guidesRoot);
        var guideSlugsByBo = GuideFrontmatterScanner.BuildGuideSlugsByBo(guides);

        var catalog = _reader.Read(moduleAssemblyPath, guideSlugsByBo);
        var navigationTree = _reader.BuildNavigationTree(catalog);
        _writer.Write(outputDirectory, catalog, navigationTree, guidesRoot);
        return catalog;
    }

    public static string ResolveDefaultModulePath(string? repoRoot = null)
    {
        repoRoot ??= FindRepoRoot();
        return Path.GetFullPath(Path.Combine(repoRoot, "Visa2026.Module", "bin", "Debug", "net8.0", "Visa2026.Module.dll"));
    }

    public static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Visa2026.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root (Visa2026.slnx).");
    }
}

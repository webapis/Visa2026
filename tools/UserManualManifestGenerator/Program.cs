// Scans Visa2026.Module for [UserDocumentation] and writes bo-catalog.json + navigation-tree.json.

using UserManualManifestGenerator;

var argsList = args.ToList();
if (argsList.Contains("--help", StringComparer.OrdinalIgnoreCase) || argsList.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("UserManualManifestGenerator");
    Console.WriteLine("Usage:");
    Console.WriteLine("  UserManualManifestGenerator --module <Visa2026.Module.dll> --output <dir> [--guides <docs-root>]");
    Console.WriteLine("  UserManualManifestGenerator --output <dir>   # uses default module path after Debug build");
    return 0;
}

var outputIndex = argsList.FindIndex(a => string.Equals(a, "--output", StringComparison.OrdinalIgnoreCase));
if (outputIndex < 0 || outputIndex + 1 >= argsList.Count)
{
    Console.Error.WriteLine("Missing required --output <directory>.");
    return 1;
}

var outputDirectory = Path.GetFullPath(argsList[outputIndex + 1]);
var moduleIndex = argsList.FindIndex(a => string.Equals(a, "--module", StringComparison.OrdinalIgnoreCase));
var modulePath = moduleIndex >= 0 && moduleIndex + 1 < argsList.Count
    ? Path.GetFullPath(argsList[moduleIndex + 1])
    : BoCatalogGenerator.ResolveDefaultModulePath();

var guidesIndex = argsList.FindIndex(a => string.Equals(a, "--guides", StringComparison.OrdinalIgnoreCase));
var guidesRoot = guidesIndex >= 0 && guidesIndex + 1 < argsList.Count
    ? Path.GetFullPath(argsList[guidesIndex + 1])
    : Path.Combine(BoCatalogGenerator.FindRepoRoot(), "user-manual", "docs");

        try
        {
            var generator = new BoCatalogGenerator();
            var catalog = generator.Generate(null, outputDirectory, guidesRoot);
    Console.WriteLine($"Catalog assembly version: {catalog.AssemblyVersion}");
    foreach (var type in catalog.Types)
        Console.WriteLine($"  {type.Name} -> {type.UserDocSlug} ({type.Properties.Count} properties)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

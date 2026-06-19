namespace Visa2026.Tools.CarboneSpike;

internal static class RepoPaths
{
    public static string Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Visa2026.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (Visa2026.slnx).");
    }

    public static string ModuleTemplates(string relativePath) =>
        Path.Combine(Root(), "Visa2026.Module", "Resources", "Templates", relativePath);

    public static string SpikeOutputDir()
    {
        var dir = Path.Combine(Root(), "tools", "CarboneSpike", "output");
        Directory.CreateDirectory(dir);
        return dir;
    }
}

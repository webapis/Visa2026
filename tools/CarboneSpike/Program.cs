using Visa2026.Module.Services.UserReports;

namespace Visa2026.Tools.CarboneSpike;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }

            return args[0].ToLowerInvariant() switch
            {
                "export-json" => ExportJson(args),
                "retag-gurlusyk" => RetagGurlusyk(),
                "inspect-gurlusyk" => InspectGurlusyk(args),
                "create-smoke-sample" => CreateSmokeSample(),
                "baseline-excel" => await BaselineExcelAsync(args),
                "baseline-word" => await BaselineWordAsync(args),
                "inject-word" => InjectWord(args),
                "paths" => PrintPaths(),
                _ => Unknown(args[0]),
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int CreateSmokeSample()
    {
        var dir = CarboneSmokeSampleFactory.CreateAll();
        Console.WriteLine($"Carbone smoke samples: {dir}");
        Console.WriteLine();
        Console.WriteLine("Studio workflow (verify Carbone before Gurlusyk):");
        Console.WriteLine("  A) HTML + PDF preview (recommended): carbone-smoke-minimal.html + carbone-smoke-minimal.json");
        Console.WriteLine("  B) XLSX: carbone-smoke-minimal.xlsx — PDF preview needs LibreOffice ({o.converter=L} in Z1)");
        Console.WriteLine("     or download merged .xlsx from toolbar instead of PDF");
        Console.WriteLine();
        Console.WriteLine("JSON is unwrapped at root. Tags use {d.field} / {d.people[i].name}.");
        return 0;
    }

    private static int InspectGurlusyk(string[] args)
    {
        var path = GetArgOptional(args, "--template")
            ?? Path.Combine(RepoPaths.Root(), "tools", "CarboneSpike", "templates", "spike", "433_gurlusyk_uzt.carbone.xlsx");
        GurlusykCarboneRetagger.Inspect(path);
        return 0;
    }

    private static int RetagGurlusyk()
    {
        var dest = GurlusykCarboneRetagger.RetagDefault();
        Console.WriteLine($"Carbone-tagged Gurlusyk template: {dest}");
        Console.WriteLine("Upload this file in Carbone Studio (not baseline-legacy-*).");
        return 0;
    }

    private static int ExportJson(string[] args)
    {
        var scenario = SpikeScenarioParser.Parse(GetArg(args, "--scenario", "gurlusyk"));
        var count = int.Parse(GetArg(args, "--items", "3"));
        var sampleRows = HasSwitch(args, "--sample-rows");
        var wrapInD = HasSwitch(args, "--wrap-d");
        var outPath = GetArgOptional(args, "--out")
            ?? Path.Combine(
                RepoPaths.SpikeOutputDir(),
                sampleRows
                    ? $"{scenario}-carbone-data-sample.json"
                    : $"{scenario}-carbone-data.json");

        var json = SpikePayloadBuilder.BuildCarboneJson(scenario, count, sampleRows, wrapInD);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, json);

        Console.WriteLine($"Scenario: {scenario}");
        Console.WriteLine($"Items: {count}");
        Console.WriteLine($"Sample rows: {sampleRows}");
        Console.WriteLine($"Wrap in {{\"d\":...}}: {wrapInD} (Studio: leave off — Carbone {{d}} is the data root alias)");
        Console.WriteLine($"Wrote Carbone JSON: {outPath}");
        Console.WriteLine("Next: upload Carbone-tagged template to Studio, paste this JSON, render, save output to tools/CarboneSpike/output/");
        return 0;
    }

    private static async Task<int> BaselineExcelAsync(string[] args)
    {
        var count = int.Parse(GetArg(args, "--items", "3"));
        var templatePath = GetArgOptional(args, "--template")
            ?? RepoPaths.ModuleTemplates(SpikeScenarioParser.TemplateRelativePath(SpikeScenario.GurlusykExcel));

        if (!File.Exists(templatePath))
            throw new FileNotFoundException(templatePath);

        var outPath = await LegacyMergeRunner.RunExcelBaselineAsync(templatePath, count);
        Console.WriteLine($"Legacy ClosedXML baseline: {outPath}");
        return 0;
    }

    private static async Task<int> BaselineWordAsync(string[] args)
    {
        var scenario = SpikeScenarioParser.Parse(GetArg(args, "--scenario", "sanaw"));
        var count = int.Parse(GetArg(args, "--items", "3"));
        var templatePath = GetArgOptional(args, "--template")
            ?? RepoPaths.ModuleTemplates(SpikeScenarioParser.TemplateRelativePath(scenario));

        if (!File.Exists(templatePath))
            throw new FileNotFoundException(templatePath);

        var outPath = await LegacyMergeRunner.RunWordBaselineAsync(templatePath, scenario, count);
        Console.WriteLine($"Legacy DocxTemplater baseline: {outPath}");
        return 0;
    }

    private static int InjectWord(string[] args)
    {
        var input = GetArg(args, "--in");
        var output = GetArgOptional(args, "--out")
            ?? Path.Combine(
                RepoPaths.SpikeOutputDir(),
                Path.GetFileNameWithoutExtension(input) + "-injected.docx");

        using var inputStream = File.OpenRead(input);
        using var outputStream = File.Create(output);
        var photos = new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_Photo"] = new List<byte[]> { SpikeSampleFactory.TinyPng },
        };
        WordUserReportImageInjector.Inject(inputStream, outputStream, photos);

        Console.WriteLine($"Injected photos → {output}");
        return 0;
    }

    private static int PrintPaths()
    {
        Console.WriteLine($"Repo: {RepoPaths.Root()}");
        Console.WriteLine($"Output: {RepoPaths.SpikeOutputDir()}");
        Console.WriteLine($"Excel template: {RepoPaths.ModuleTemplates(SpikeScenarioParser.TemplateRelativePath(SpikeScenario.GurlusykExcel))}");
        Console.WriteLine($"Sanaw template: {RepoPaths.ModuleTemplates(SpikeScenarioParser.TemplateRelativePath(SpikeScenario.SanawWord))}");
        Console.WriteLine($"Forma 16 template: {RepoPaths.ModuleTemplates(SpikeScenarioParser.TemplateRelativePath(SpikeScenario.Forma16Word))}");
        return 0;
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 2;
    }

    private static string GetArg(string[] args, string name, string? defaultValue = null)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        if (defaultValue != null)
            return defaultValue;

        throw new ArgumentException($"Missing {name}");
    }

    private static string? GetArgOptional(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static bool HasSwitch(string[] args, string name) =>
        args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    private static void PrintHelp()
    {
        Console.WriteLine("""
            CarboneSpike — Phase 0 tooling (no schema changes)

            Commands:
              export-json [--scenario gurlusyk|sanaw|forma16] [--items N] [--sample-rows] [--wrap-d] [--out path]
              create-smoke-sample
              baseline-excel [--items N] [--template path]
              baseline-word [--scenario sanaw|forma16] [--items N] [--template path]
              inject-word --in merged.docx [--out path]
              paths

            Workflow:
              1. export-json → paste into Carbone Studio with Carbone-tagged template copy
              2. baseline-* → legacy merge for side-by-side comparison
              3. inject-word → apply {{IMAGE:Person_Photo}} after Carbone DOCX merge

            See tools/CarboneSpike/README.md
            """);
    }
}

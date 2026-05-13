using DocxTemplater;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;

/// <summary>
/// Fills Word templates under Visa2026.Module/Resources with static sample data so designers
/// can open the result in Word without running the Blazor app. Uses the same DocxTemplater bind
/// rules as Visa2026.Module.Services.WordFormFillerService (model prefix "ds").
/// </summary>
static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "/?")
        {
            PrintHelp();
            return args.Length == 0 ? 1 : 0;
        }

        var cmd = args[0].Trim().ToLowerInvariant();
        if (cmd is "list" or "presets")
        {
            foreach (var name in Presets.Keys.OrderBy(s => s))
                Console.WriteLine($"  {name}");
            return 0;
        }

        if (!Presets.TryGetValue(cmd, out var preset))
        {
            Console.Error.WriteLine($"Unknown preset '{args[0]}'. Run with argument 'list' to see names.");
            return 1;
        }

        var resourcesDir = FindModuleResourcesDirectory();
        var templatePath = Path.Combine(resourcesDir, preset.TemplateFileName);
        if (!File.Exists(templatePath))
        {
            Console.Error.WriteLine($"Template not found: {templatePath}");
            return 1;
        }

        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, preset.OutputFileName);

        using (var templateStream = File.OpenRead(templatePath))
        using (var outStream = File.Create(outPath))
        {
            if (preset.UseListForm)
            {
                var model = new Dictionary<string, object>(preset.Header) { ["rows"] = preset.Rows! };
                var template = new DocxTemplate(templateStream);
                template.BindModel("ds", model);
                template.Save(outStream);
            }
            else
            {
                var template = new DocxTemplate(templateStream);
                template.BindModel("ds", preset.SingleData!);
                template.Save(outStream);
            }
        }

        ApplyDumpDataHighlights(outPath, preset);

        Console.WriteLine(outPath);
        return 0;
    }

    /// <summary>
    /// Yellow highlight on text that matches bound sample values so dynamic fields are obvious.
    /// Composites cover "pasporty …" paragraphs where the label is static in the template.
    /// </summary>
    static void ApplyDumpDataHighlights(string docxPath, PresetDef preset)
    {
        var composites = new List<string>();
        if (preset.Rows is not null)
        {
            foreach (var row in preset.Rows)
            {
                if (row.TryGetValue("CompanyHead_PassportLine", out var o) && o is string p && p.Trim().Length > 0)
                    composites.Add("pasporty " + p.Trim());
                if (row.TryGetValue("Representative_PassportLine", out var o2) && o2 is string p2 && p2.Trim().Length > 0)
                    composites.Add("pasporty " + p2.Trim());
            }
        }

        var set = DumpDataHighlighter.CollectMatchStrings(
            preset.Header,
            preset.SingleData,
            preset.Rows,
            composites);

        if (set.Count == 0)
            return;

        using var doc = WordprocessingDocument.Open(docxPath, true);
        var main = doc.MainDocumentPart;
        if (main?.Document?.Body is null)
            return;
        DumpDataHighlighter.ApplyToDocument(main, set);
    }

    /// <summary>Post-merge: yellow highlight on runs matching sample bind strings (preview only).</summary>
    static class DumpDataHighlighter
    {
        public static void ApplyToDocument(MainDocumentPart mainPart, IReadOnlySet<string> matchStrings)
        {
            if (matchStrings.Count == 0)
                return;

            var body = mainPart.Document?.Body;
            if (body is null)
                return;

            const int minLen = 4;
            var ordered = matchStrings
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length >= minLen)
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(s => s.Length)
                .ToList();

            var highlightedParagraphs = new HashSet<Paragraph>();

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var full = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text ?? string.Empty));
                var trim = full.Trim();
                if (trim.Length < minLen)
                    continue;

                foreach (var candidate in ordered)
                {
                    if (trim.Equals(candidate, StringComparison.Ordinal))
                    {
                        foreach (var run in paragraph.Descendants<Run>())
                            AddYellowHighlight(run);
                        highlightedParagraphs.Add(paragraph);
                        break;
                    }
                }
            }

            foreach (var run in body.Descendants<Run>())
            {
                if (run.Ancestors<Paragraph>().Any(p => highlightedParagraphs.Contains(p)))
                    continue;

                var text = string.Concat(run.Elements<Text>().Select(t => t.Text ?? string.Empty)).Trim();
                if (text.Length < minLen)
                    continue;

                foreach (var candidate in ordered)
                {
                    if (text.Equals(candidate, StringComparison.Ordinal))
                    {
                        AddYellowHighlight(run);
                        break;
                    }
                }
            }

            mainPart.Document!.Save();
        }

        static void AddYellowHighlight(Run run)
        {
            var rPr = run.GetFirstChild<RunProperties>();
            if (rPr is null)
            {
                rPr = new RunProperties();
                run.InsertAt(rPr, 0);
            }

            if (rPr.GetFirstChild<Highlight>() is null)
                rPr.AppendChild(new Highlight { Val = HighlightColorValues.Yellow });

            if (rPr.GetFirstChild<Shading>() is null)
                rPr.AppendChild(new Shading
                {
                    Val = ShadingPatternValues.Clear,
                    Color = "auto",
                    Fill = "FFF59C"
                });
        }

        public static HashSet<string> CollectMatchStrings(
            IReadOnlyDictionary<string, object> header,
            IReadOnlyDictionary<string, object>? single,
            IEnumerable<Dictionary<string, object>>? rows,
            IReadOnlyList<string>? compositeLines)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            void AddDict(IReadOnlyDictionary<string, object> d)
            {
                foreach (var v in d.Values)
                {
                    if (v is string s)
                    {
                        var t = s.Trim();
                        if (t.Length >= 4)
                            set.Add(t);
                    }
                }
            }

            AddDict(header);
            if (single is not null)
                AddDict(single);
            if (rows is not null)
            {
                foreach (var row in rows)
                    AddDict(row);
            }

            if (compositeLines is not null)
            {
                foreach (var line in compositeLines)
                {
                    var t = line.Trim();
                    if (t.Length >= 4)
                        set.Add(t);
                }
            }

            return set;
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("""
            Fill Word templates with dump data (no XAF / no Blazor).

            Usage:
              dotnet run --project tools/PreviewWordReports -- <preset>
              dotnet run --project tools/PreviewWordReports -- list

            Templates are read from Visa2026.Module/Resources (walks up from bin until that folder exists).
            Output is written next to the EXE under ./out/<preset>_preview.docx
            Dynamic field text is highlighted in yellow (sample data only).

            Add new presets in Program.cs (Presets dictionary).
            """);
    }

    /// <summary>Walks parent directories from the EXE until <c>Visa2026.Module/Resources</c> exists.</summary>
    static string FindModuleResourcesDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "Visa2026.Module", "Resources");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find Visa2026.Module/Resources. Run from the repo after building this project (or set working directory to repo root).");
    }

    sealed record PresetDef(
        string TemplateFileName,
        string OutputFileName,
        bool UseListForm,
        IReadOnlyDictionary<string, object>? SingleData,
        IReadOnlyDictionary<string, object> Header,
        IEnumerable<Dictionary<string, object>>? Rows);

    static readonly IReadOnlyDictionary<string, PresetDef> Presets = new Dictionary<string, PresetDef>(StringComparer.OrdinalIgnoreCase)
    {
        ["borcnama"] = new PresetDef(
            TemplateFileName: "App_Inv_And_WP_Borcnama_Item.docx",
            OutputFileName: "borcnama_preview.docx",
            UseListForm: true,
            SingleData: null,
            Header: new Dictionary<string, object>(),
            Rows: BorcnamaSampleRows()),

        ["business-trip-arrival-letter"] = new PresetDef(
            TemplateFileName: "BusinessTrip_Arrival_Letter.docx",
            OutputFileName: "business_trip_arrival_letter_preview.docx",
            UseListForm: false,
            SingleData: BusinessTripArrivalLetterData(),
            Header: new Dictionary<string, object>(),
            Rows: null),

        ["business-trip-departure-letter"] = new PresetDef(
            TemplateFileName: "BusinessTrip_Departure_Letter.docx",
            OutputFileName: "business_trip_departure_letter_preview.docx",
            UseListForm: false,
            SingleData: BusinessTripDepartureLetterData(),
            Header: new Dictionary<string, object>(),
            Rows: null),
    };

    static IEnumerable<Dictionary<string, object>> BorcnamaSampleRows()
    {
        // One row → one-page preview; add a second yield to QA {{:s:}}{{:PageBreak}} between persons.
        yield return new Dictionary<string, object>
        {
            ["Person_FullName"] = "Mehmet Aksoy",
            ["Person_DateOfBirthText"] = "30.08.1988ý.",
            ["Application_SponsorName"] = "Çalyk Enerji Sanaýi we Tijaret A.Ş. Türk kärhanasynyň Türkmenistandaky şahamçasy",
            ["Application_CompanyRegistryAddressLine"] = "№2634070902.02.02.2009ý. Aşgabat ş., Bitarap Türkmenistan şaýoly 538, +993 12 75-57-58",
            ["CompanyHead_FullName"] = "Mehmet ÇIRAK",
            ["CompanyHead_PassportLine"] = "U37109249, T.C. ASKABET BE, 19.02.2024ý.",
            ["Representative_FullName"] = "Nepesowa Tumar Aşyrowna",
            ["Representative_PassportLine"] = "I-AŞ 476479 Aşgabat ş., Berkararlyk etr. Häkimligi tarapyndan berlen, +993 65 56-13-49",
        };
    }

    static Dictionary<string, object> BusinessTripArrivalLetterData() => new()
    {
        ["FullApplicationNumber"] = "CEC-0042/2026",
        ["ApplicationDate"] = "12.05.2026",
        ["MigrationService_NameTm"] = "Türkmenistanyň Döwlet migrasiýa gullugyna",
        ["TotalPersonCountText"] = "üç",
        ["TotalPersonCount"] = 3,
        ["BusinessTripStartDateText"] = "01.06.2026",
        ["BusinessTripEndDateText"] = "15.06.2026",
        ["BusinessTripDurationDays"] = 14,
        ["ToRegionName_Genitive"] = "Mary welaýatyna",
        ["ToCityName_Dative"] = "Mary şäherine",
        ["BusinessTripPurpose_NameTm"] = "Tehniki hyzmatdaşlyk",
        ["Application_CompanyHead_PositionTm"] = "Direktor",
        ["Application_CompanyHead_FullName"] = "Aman Amanow",
    };

    static Dictionary<string, object> BusinessTripDepartureLetterData() => new()
    {
        ["FullApplicationNumber"] = "CEC-0042/2026",
        ["ApplicationDate"] = "12.05.2026",
        ["MigrationService_NameTm"] = "Türkmenistanyň Döwlet migrasiýa gullugyna",
        ["TotalPersonCountText"] = "üç",
        ["TotalPersonCount"] = 3,
        ["BusinessTripStartDateText"] = "01.06.2026",
        ["BusinessTripEndDateText"] = "15.06.2026",
        ["BusinessTripDurationDays"] = 14,
        ["ToRegionName_Genitive"] = "Mary welaýatyna",
        ["ProjectContract_Description"] = "Elektrik enjamlarynyň montajy we işläp düşürilmegi",
        ["Application_CompanyHead_PositionTm"] = "Direktor",
        ["Application_CompanyHead_FullName"] = "Aman Amanow",
    };
}

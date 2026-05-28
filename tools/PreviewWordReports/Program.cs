using DocxTemplater;
using DocumentFormat.OpenXml;
using Visa2026.Module.Services.UserReports;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

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

        var outDir = Path.Combine(AppContext.BaseDirectory, "out");
        Directory.CreateDirectory(outDir);

        string templatePath;
        if (string.Equals(cmd, "employee-photo-roster", StringComparison.OrdinalIgnoreCase))
        {
            templatePath = Path.Combine(outDir, "employee_photo_roster_template.docx");
            WriteEmployeePhotoRosterTemplate(templatePath);
        }
        else
        {
            templatePath = ResolveTemplatePath(preset.TemplateFileName);
            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine($"Template not found: {templatePath}");
                return 1;
            }
        }
        var outPath = Path.Combine(outDir, preset.OutputFileName);

        using (var templateStream = File.OpenRead(templatePath))
        using (var outStream = OpenPreviewOutputStream(ref outPath))
        {
            var template = OpenTemplate(templateStream);
            var bindData = BuildBindModel(preset);
            template.BindModel("ds", bindData);

            using var merged = new MemoryStream();
            try
            {
                template.Save(merged);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }

            merged.Position = 0;
            var photosByKey = WordUserReportMergeImageExtractor.FromBindData(bindData);
            if (photosByKey.Count > 0)
            {
                using var injected = new MemoryStream();
                WordUserReportImageInjector.Inject(merged, injected, photosByKey);
                injected.Position = 0;
                injected.CopyTo(outStream);
            }
            else
            {
                merged.CopyTo(outStream);
            }
        }

        ApplyDumpDataHighlights(outPath, preset);

        Console.WriteLine(outPath);
        return 0;
    }

    static DocxTemplate OpenTemplate(Stream templateStream) => DocxTemplateFactory.Open(templateStream);

    static Dictionary<string, object> BuildBindModel(PresetDef preset)
    {
        var model = new Dictionary<string, object>(preset.Header);
        if (preset.UseListForm && preset.Rows is not null)
            model["rows"] = preset.Rows.ToList();
        else if (!string.IsNullOrEmpty(preset.CollectionName) && preset.CollectionItems is not null)
            model[preset.CollectionName] = preset.CollectionItems.ToList();
        else if (preset.SingleData is not null)
        {
            foreach (var kv in preset.SingleData)
                model[kv.Key] = kv.Value;
        }

        return model;
    }

    /// <summary>Creates the output file; if the default path is locked (e.g. open in Word), uses a timestamped sibling name.</summary>
    static Stream OpenPreviewOutputStream(ref string outPath)
    {
        try
        {
            return new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
        catch (IOException)
        {
            var dir = Path.GetDirectoryName(outPath)!;
            var name = Path.GetFileNameWithoutExtension(outPath);
            var ext = Path.GetExtension(outPath);
            outPath = Path.Combine(dir, $"{name}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
            Console.Error.WriteLine($"Warning: default preview file was locked; wrote instead to:{Environment.NewLine}  {outPath}");
            return new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }

    /// <summary>
    /// Phrases built from multiple bind keys so merged output (e.g. <c>2 (iki)</c>, date + <c> ý.</c>) still matches.
    /// </summary>
    static void AddSingleDataComposites(IReadOnlyDictionary<string, object>? single, List<string> composites)
    {
        if (single is null)
            return;

        string? nStr = null;
        if (single.TryGetValue("TotalPersonCount", out var nObj))
            nStr = Convert.ToString(nObj, CultureInfo.InvariantCulture)?.Trim();

        if (!string.IsNullOrEmpty(nStr) &&
            single.TryGetValue("TotalPersonCountText", out var tObj))
        {
            var tStr = (tObj as string)?.Trim();
            if (!string.IsNullOrEmpty(tStr))
            {
                composites.Add($"{nStr} ({tStr})");
                composites.Add($"{nStr}-daşary");
                composites.Add($"{nStr}-pasport");
            }
        }

        // Goşundy-style letters: merged "– {n} sany" / "( {n} sany" — single-digit n is not added from int in CollectMatchStrings.
        if (!string.IsNullOrEmpty(nStr))
        {
            composites.Add("\u2013 " + nStr + " sany");
            composites.Add("( " + nStr + " sany");
            composites.Add("pasport nusgalary \u2013 " + nStr + " sany");
        }

        if (single.TryGetValue("ApplicationDate", out var dObj) && dObj is string ds && ds.Trim().Length > 0)
            composites.Add(ds.Trim() + " ý.");

        if (single.TryGetValue("CancelPersonCount", out var cObj))
        {
            var cStr = Convert.ToString(cObj, CultureInfo.InvariantCulture)?.Trim();
            if (!string.IsNullOrEmpty(cStr) &&
                single.TryGetValue("CancelPersonCountText", out var ctObj))
            {
                var ctStr = (ctObj as string)?.Trim();
                if (!string.IsNullOrEmpty(ctStr))
                    composites.Add($"{cStr} ({ctStr})");
            }
        }

        if (single.TryGetValue("VisaPeriod_NameTm", out var vpObj) &&
            single.TryGetValue("VisaCategory_NameTm", out var vcObj))
        {
            var vp = (vpObj as string)?.Trim();
            var vc = (vcObj as string)?.Trim();
            if (!string.IsNullOrEmpty(vp) && !string.IsNullOrEmpty(vc))
                composites.Add(vp + " " + vc);
        }
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

        AddSingleDataComposites(preset.SingleData, composites);

        var set = DumpDataHighlighter.CollectMatchStrings(
            preset.Header,
            preset.SingleData,
            preset.Rows,
            preset.CollectionItems,
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
        const int MinCandidateLength = 2;

        public static void ApplyToDocument(MainDocumentPart mainPart, IReadOnlySet<string> matchStrings)
        {
            if (matchStrings.Count == 0)
                return;

            var body = mainPart.Document?.Body;
            if (body is null)
                return;

            var ordered = matchStrings
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Trim().Length >= MinCandidateLength)
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(s => s.Length)
                .ToList();

            if (ordered.Count == 0)
                return;

            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var full = GetParagraphPlainText(paragraph);
                if (full.Length < MinCandidateLength)
                    continue;

                var intervals = CollectNonOverlappingMatchIntervals(full, ordered);
                if (intervals.Count == 0)
                    continue;

                var spans = GetRunCharSpans(paragraph);
                foreach (var (run, rs, re) in spans)
                {
                    if (intervals.Any(iv => iv.start < re && iv.end > rs))
                        AddYellowHighlight(run);
                }
            }

            mainPart.Document!.Save();
        }

        /// <summary>Map <c>w:br</c> / tab to chars so preset newlines align with merged OOXML.</summary>
        static string GetParagraphPlainText(Paragraph p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var run in p.Elements<Run>())
            {
                foreach (var child in run.ChildElements)
                {
                    switch (child)
                    {
                        case Text tx:
                            sb.Append(tx.Text);
                            break;
                        case Break:
                            sb.Append('\n');
                            break;
                        case TabChar:
                            sb.Append(' ');
                            break;
                    }
                }
            }
            return sb.ToString();
        }

        static List<(Run run, int start, int end)> GetRunCharSpans(Paragraph p)
        {
            var list = new List<(Run run, int start, int end)>();
            var pos = 0;
            foreach (var run in p.Elements<Run>())
            {
                var runStart = pos;
                foreach (var child in run.ChildElements)
                {
                    switch (child)
                    {
                        case Text tx:
                            pos += (tx.Text ?? "").Length;
                            break;
                        case Break:
                            pos += 1;
                            break;
                        case TabChar:
                            pos += 1;
                            break;
                    }
                }
                if (pos > runStart)
                    list.Add((run, runStart, pos));
            }
            return list;
        }

        /// <summary>Longest candidates first; each match must not overlap already-claimed character ranges.</summary>
        static List<(int start, int end)> CollectNonOverlappingMatchIntervals(string full, List<string> orderedCandidates)
        {
            var claimed = new List<(int start, int end)>();
            foreach (var cand in orderedCandidates)
            {
                if (cand.Length > full.Length)
                    continue;
                for (var pos = 0; pos <= full.Length - cand.Length;)
                {
                    var idx = full.IndexOf(cand, pos, StringComparison.Ordinal);
                    if (idx < 0)
                        break;
                    var end = idx + cand.Length;
                    if (!claimed.Any(c => c.start < end && c.end > idx))
                        claimed.Add((idx, end));
                    pos = idx + 1;
                }
            }
            return claimed;
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
            IEnumerable<object>? collectionItems,
            IReadOnlyList<string>? compositeLines)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);

            void AddValue(object? v)
            {
                switch (v)
                {
                    case bool:
                        return;
                    case string s:
                    {
                        var t = s.Trim();
                        if (t.Length >= MinCandidateLength)
                            set.Add(t);
                        break;
                    }
                    case int:
                    case long:
                    case short:
                    case uint:
                    case ulong:
                    case ushort:
                    {
                        var xs = Convert.ToString(v, CultureInfo.InvariantCulture)!;
                        if (xs.Length >= 2)
                            set.Add(xs);
                        break;
                    }
                    case decimal x:
                        set.Add(x.ToString(CultureInfo.InvariantCulture));
                        break;
                    case double:
                    case float:
                    {
                        var xs = Convert.ToString(v, CultureInfo.InvariantCulture)!;
                        if (xs.Length >= MinCandidateLength)
                            set.Add(xs);
                        break;
                    }
                    case byte[]:
                        return;
                    default:
                    {
                        var xs = Convert.ToString(v, CultureInfo.InvariantCulture)?.Trim();
                        if (!string.IsNullOrEmpty(xs) && xs.Length >= MinCandidateLength)
                            set.Add(xs);
                        break;
                    }
                }
            }

            void AddDict(IReadOnlyDictionary<string, object> d)
            {
                foreach (var v in d.Values)
                    AddValue(v);
            }

            AddDict(header);
            if (single is not null)
                AddDict(single);
            if (rows is not null)
            {
                foreach (var row in rows)
                    AddDict(row);
            }

            if (collectionItems is not null)
            {
                foreach (var item in collectionItems)
                {
                    if (item is PhotoRosterMergeRow row)
                        AddValue(row.Person_FullName);
                }
            }

            if (compositeLines is not null)
            {
                foreach (var line in compositeLines)
                {
                    var t = line.Trim();
                    if (t.Length >= MinCandidateLength)
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
            Dynamic field text is highlighted in yellow only in files produced by this tool under ./out (not in Blazor / Resminamalar downloads).

            Add new presets in Program.cs (Presets dictionary).

            Employee photo roster (user template):
              dotnet run --project tools/PreviewWordReports -- employee-photo-roster
            Uses PNGs from tools/PreviewWordReports/SamplePhotos/; photos injected via WordUserReportImageInjector ({{IMAGE:Person_Photo}}).
            """);
    }

    /// <summary>Row model for <c>{{#ds.ApplicationItems}}</c> + <c>{{IMAGE:Person_Photo}}</c> (typed <c>byte[]</c>, not a dictionary).</summary>
    sealed class PhotoRosterMergeRow
    {
        public string Person_FullName { get; init; } = string.Empty;
        public byte[] Person_Photo { get; init; } = Array.Empty<byte>();
    }

    /// <summary>Walks parent directories from the EXE until <c>tools/PreviewWordReports/SamplePhotos</c> exists.</summary>
    static string FindSamplePhotosDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "tools", "PreviewWordReports", "SamplePhotos");
            if (Directory.Exists(candidate))
                return candidate;
            candidate = Path.Combine(dir.FullName, "SamplePhotos");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find tools/PreviewWordReports/SamplePhotos. Add demo PNGs there or run from the repo root.");
    }

    static byte[] LoadSamplePhotoPng(string fileName)
    {
        var path = Path.Combine(FindSamplePhotosDirectory(), fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Sample photo not found: {path}");
        // PNG bytes for WordUserReportImageInjector after DocxTemplater text merge.
        return File.ReadAllBytes(path);
    }

    /// <summary>3×4 cm at 96 DPI (113×151), same target as <c>Person.OnSaving</c> for mail-merge-sized output.</summary>
    static byte[] NormalizePassportPhoto(byte[] imageBytes, int targetWidth = 113, int targetHeight = 151)
    {
        using var input = new MemoryStream(imageBytes);
        using var img = Image.FromStream(input);
        float targetRatio = (float)targetWidth / targetHeight;
        int sourceWidth = img.Width;
        int sourceHeight = img.Height;
        float sourceRatio = (float)sourceWidth / sourceHeight;
        int cropX = 0, cropY = 0, cropWidth = sourceWidth, cropHeight = sourceHeight;
        if (sourceRatio > targetRatio)
        {
            cropWidth = (int)(sourceHeight * targetRatio);
            cropX = (sourceWidth - cropWidth) / 2;
        }
        else if (sourceRatio < targetRatio)
        {
            cropHeight = (int)(sourceWidth / targetRatio);
            cropY = (sourceHeight - cropHeight) / 2;
        }

        using var newImg = new Bitmap(targetWidth, targetHeight);
        newImg.SetResolution(96f, 96f);
        using (var g = Graphics.FromImage(newImg))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.DrawImage(img, new Rectangle(0, 0, targetWidth, targetHeight),
                new Rectangle(cropX, cropY, cropWidth, cropHeight), GraphicsUnit.Pixel);
        }

        using var output = new MemoryStream();
        newImg.Save(output, ImageFormat.Png);
        return output.ToArray();
    }

    /// <summary>Rebuilds the seed <c>.docx</c> with valid Open XML (Word-created structure) so DocxTemplater parses placeholders.</summary>
    static void WriteEmployeePhotoRosterTemplate(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());

            static Paragraph P(string text, bool bold = false, JustificationValues? just = null, int sz = 22, int spaceAfter = 0)
            {
                var rpr = new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = sz.ToString() });
                if (bold) rpr.AppendChild(new Bold());
                var ppr = new ParagraphProperties(new Justification { Val = just ?? JustificationValues.Left });
                if (spaceAfter > 0)
                    ppr.AppendChild(new SpacingBetweenLines { After = spaceAfter.ToString() });
                return new Paragraph(ppr, new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
            }

            static TableCell Cell(string text, int width, TableVerticalAlignmentValues valign, JustificationValues halign)
            {
                var tcPr = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() },
                    new TableCellVerticalAlignment { Val = valign });
                var p = new Paragraph(
                    new ParagraphProperties(new Justification { Val = halign }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                            new FontSize { Val = "22" }),
                        new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
                return new TableCell(tcPr, p);
            }

            body.AppendChild(P("Employee photo roster (sample)", bold: true, just: JustificationValues.Center, sz: 24));
            body.AppendChild(P("Application: {{ds.FullApplicationNumber}}", spaceAfter: 120));
            body.AppendChild(P("{{#ds.ApplicationItems}}", sz: 1));

            var table = new Table(
                new TableProperties(
                    new TableWidth { Type = TableWidthUnitValues.Dxa, Width = "9000" },
                    new TableLayout { Type = TableLayoutValues.Fixed },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new RightBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                        new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })),
                new TableGrid(new GridColumn { Width = "4500" }, new GridColumn { Width = "4500" }),
                new TableRow(
                    Cell("{{.Person_FullName}}", 4500, TableVerticalAlignmentValues.Center, JustificationValues.Left),
                    Cell("{{IMAGE:Person_Photo}}", 4500, TableVerticalAlignmentValues.Center, JustificationValues.Center)));

            body.AppendChild(table);
            body.AppendChild(new Paragraph(new Run(new Text("{{:s:}}{{:PageBreak}}") { Space = SpaceProcessingModeValues.Preserve })));
            body.AppendChild(P("{{/ds.ApplicationItems}}", sz: 1));
            body.AppendChild(new SectionProperties(
                new PageSize { Width = 11906, Height = 16838 },
                new PageMargin { Top = 720, Right = 1020, Bottom = 720, Left = 1020 }));
            main.Document.Save();
        }

        File.WriteAllBytes(outputPath, ms.ToArray());
    }

    static IEnumerable<object> EmployeePhotoRosterSampleRows()
    {
        yield return Row("Ali Zengin", "ali_zengin.png");
        yield return Row("Adnan Tufan", "adnan_tufan.png");
        yield return Row("Alik Murat Kurt", "alik_murat_kurt.png");
        yield return Row("Alper Unluer", "alper_unluer.png");
        yield return Row("No Photo (sample)", photoFile: null);
    }

    static PhotoRosterMergeRow Row(string fullName, string? photoFile)
    {
        return new PhotoRosterMergeRow
        {
            Person_FullName = fullName,
            Person_Photo = photoFile is null ? Array.Empty<byte>() : LoadSamplePhotoPng(photoFile),
        };
    }

    /// <summary>Module embedded templates first; then <c>tools/PreviewWordReports/Templates</c> for tool-only samples.</summary>
    static string ResolveTemplatePath(string templateFileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 14 && dir is not null; i++)
        {
            var local = Path.Combine(dir.FullName, "tools", "PreviewWordReports", "Templates", templateFileName);
            if (File.Exists(local))
                return local;
            dir = dir.Parent;
        }

        return Path.Combine(FindModuleResourcesDirectory(), templateFileName);
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
        IEnumerable<Dictionary<string, object>>? Rows,
        string? CollectionName = null,
        IEnumerable<object>? CollectionItems = null);

    static readonly IReadOnlyDictionary<string, PresetDef> Presets = new Dictionary<string, PresetDef>(StringComparer.OrdinalIgnoreCase)
    {
        ["borcnama"] = new PresetDef(
            TemplateFileName: @"Templates\Borcnama.docx",
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

        ["inv-and-wp-letter"] = new PresetDef(
            TemplateFileName: @"Templates\GT-15_Elyasow_ckl_only.docx",
            OutputFileName: "inv_and_wp_letter_preview.docx",
            UseListForm: false,
            SingleData: InvAndWPLetterData(),
            Header: new Dictionary<string, object>(),
            Rows: null),

        ["labor-contract"] = new PresetDef(
            TemplateFileName: @"Templates\Contract_uzt.docx",
            OutputFileName: "labor_contract_preview.docx",
            UseListForm: true,
            SingleData: null,
            Header: new Dictionary<string, object>(),
            Rows: LaborContractSampleRows()),

        ["energy-to-construction-ministry-letter"] = new PresetDef(
            TemplateFileName: @"Templates\GT-15_MINSTROY_uzt.docx",
            OutputFileName: "energy_to_construction_ministry_letter_preview.docx",
            UseListForm: false,
            SingleData: EnergyToConstructionMinistryLetterData(),
            Header: new Dictionary<string, object>(),
            Rows: null),

        ["visa-wp-ext-gt15-calik-migration"] = new PresetDef(
            TemplateFileName: @"Templates\GT-15_Sazakow_uzt.docx",
            OutputFileName: "visa_wp_ext_gt15_calik_migration_preview.docx",
            UseListForm: false,
            SingleData: CalikGt15MigrationLetterData(),
            Header: new Dictionary<string, object>(),
            Rows: null),

        ["employee-photo-roster"] = new PresetDef(
            TemplateFileName: @"out\employee_photo_roster_template.docx",
            OutputFileName: "employee_photo_roster_preview.docx",
            UseListForm: false,
            SingleData: null,
            Header: new Dictionary<string, object> { ["FullApplicationNumber"] = "3/-433" },
            Rows: null,
            CollectionName: "ApplicationItems",
            CollectionItems: EmployeePhotoRosterSampleRows()),
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

    static IEnumerable<Dictionary<string, object>> LaborContractSampleRows()
    {
        // Sample data matching App_Labor_Contract_item.png scan (transcribed values)
        yield return new Dictionary<string, object>
        {
            ["Application_SponsorName"] = "Çalyk Enerji Sanaýi we Tijaret A.Ş.",
            ["Application_SponsorSignatory"] = "Mehmet Ali Çalık",
            ["Application_CompanyAddress"] = "Aşgabat şäheri, Arçabil şaýoly 58. tel: 12-34-56",
            ["Person_FullName"] = "Azat Berdimuhamedow",
            ["Position_PositionTm"] = "Inžener",
            ["Passport_Number"] = "A12345678",
            ["Contract_StartDateText"] = "01.01.2026",
            ["Contract_ExpirationDateText"] = "31.12.2026",
            ["Contract_PeriodFallbackText"] = "01.01.2026 - 31.12.2026",
            ["Contract_SalaryText"] = "5.000",
            ["Salary_CurrencyCode"] = "USD",
        };
        // Add second row to test {{:s:}}{{:PageBreak}} between items
        yield return new Dictionary<string, object>
        {
            ["Application_SponsorName"] = "Çalyk Enerji Sanaýi we Tijaret A.Ş.",
            ["Application_SponsorSignatory"] = "Mehmet Ali Çalık",
            ["Application_CompanyAddress"] = "Aşgabat şäheri, Arçabil şaýoly 58. tel: 12-34-56",
            ["Person_FullName"] = "Ahmet Yilmaz",
            ["Position_PositionTm"] = "Ussat",
            ["Passport_Number"] = "B87654321",
            ["Contract_StartDateText"] = "01.01.2026",
            ["Contract_ExpirationDateText"] = "31.12.2026",
            ["Contract_PeriodFallbackText"] = "01.01.2026 - 31.12.2026",
            ["Contract_SalaryText"] = "6.500",
            ["Salary_CurrencyCode"] = "USD",
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

    static Dictionary<string, object> InvAndWPLetterData() => new()
    {
        ["FullApplicationNumber"] = "№ 1/-2",
        ["ApplicationDate"] = "02.01.2026",
        ["ApplicationDateText"] = "02.01.2026",
        ["ProjectContract_Ministry_RecipientBlock"] =
            "\"Türkmenenergo\" döwlet elektroenergetika korporasiýasynyň başlygy  D. Elýasowa",
        ["ProjectContract_Ministry_RecipientBlock_Line1"] =
            "\"Türkmenenergo\" döwlet elektroenergetika",
        ["ProjectContract_Ministry_RecipientBlock_Line2"] =
            "korporasiýasynyň başlygy  D. Elýasowa",
        ["ProjectContract_Ministry_RecipientBlock_HasLine2"] = true,
        ["Urgency_NameTm"] = "Gyssagly tertipde!",
        ["ProjectContract_Ministry_FormOfAddress"] = "Hormatly Durdy Baýjanowiç!",
        // Same narrative as seeded GT-15 ProjectContract.Description (lookup seed / data importer).
        ["ProjectContract_Description"] =
            "Türkmenistanyň Prezidentiniň 28.10.2023ý. seneli, 754 belgili kararyna laýyklykda, Türkmenistanyň Energetika ministrliginiň \"Türkmenenergo\" döwlet elektroenergetika korporasiýasy bilen Türkiýe Respublikasynyň “Çalık Enerji Senagýi ve Ticaret A.Ş” kompaniýasynyň arasynda “Balkan welaýatyndaky Türkmenbaşydaky elektrik beketiniň kuwwatlylygy 1574 MW bolup ulanmaga taýýarlanýan döwrebap elektrik stansiýasynyň we ony energogiňan birleşdirilmeginiň ikin geçir bolan elektrik geçiriji ulgamyň gurmak hem-de bar bolan döwletiň elektrik stansiýalary üçin zerur bolan taýýarlyk şaýatlaryny satyn almak” hakyndaky GT-15 belgili şertnama 01.12.2023ý. senesinde baglaşyldy.",
        ["Company_Name"] = "Çalık Enerji Sanayi ve Ticaret A.Ş.",
        ["TotalPersonCount"] = 2,
        ["TotalPersonCountText"] = "iki",
        ["VisaPeriod_NameTm"] = "6 (alty) aý",
        ["VisaCategory_NameTm"] = "işçi",
        ["Application_CompanyHead_PositionTm"] = "Türkmenistandaky şahamçasynyň müdiri",
        ["Application_CompanyHead_FullName"] = "Mehmet ÇIRAK",
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

    /// <summary>Scan-derived values for <c>App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter.docx</c> (yellow fields).</summary>
    static IReadOnlyDictionary<string, object> EnergyToConstructionMinistryLetterData() =>
        new Dictionary<string, object>
        {
            ["CancelPersonCount"] = 18,
            ["CancelPersonCountText"] = "on sekiz",
            ["VisaPeriod_NameTm"] = "6 (alty) aý köp gezeklik",
        };

    /// <summary>Scan-aligned sample for <c>App_Visa_And_WP_Ext_GT15_Calik_Migration_Letter.docx</c>.</summary>
    static IReadOnlyDictionary<string, object> CalikGt15MigrationLetterData() =>
        new Dictionary<string, object>
        {
            ["FullApplicationNumber"] = "№ 4/-434",
            ["ApplicationDate"] = "28.04.2026",
            ["MigrationService_NameTm"] = "Türkmenistanyň Döwlet migrasiýa gullugyna",
            ["ApplicationType_ShowUrgency"] = true,
            ["Urgency_NameTm"] = "Adaty tertipde!",
            ["ProjectContract_Description"] =
                "Türkmenistanyň Prezidentiniň 28.10.2023ý. seneli, 754 belgili kararyna laýyklykda, Türkmenistanyň Energetika ministrliginiň \"Türkmenenergo\" döwlet elektroenergetika korporasiýasy bilen Türkiýe Respublikasynyň \"Çalık Enerji Sanaýi ve Ticaret A.Ş\" kompaniýasynyň arasynda \"Balkan welaýatynyň Türkmenbaşy etrabynda kuwwatlylygy 1574 MWt bolan utgaşykly dolanyşykda işleýän elektrik stansiýasyny we ony energoulgama birikdirmek üçin gerek bolan elektrik geçiriji ulgamlary gurmak hem-de bar bolan döwlet elektrik stansiýalary üçin zerur bolan ätiýaçlyk şaýlaryny satyn almak\" hakyndaky GT-15 belgili şertnama 01.12.2023ý. senesinde baglaşyldy.",
            ["CancelPersonCount"] = 18,
            ["CancelPersonCountText"] = "on sekiz",
            ["VisaPeriod_NameTm"] = "6 (alty) aý",
            ["VisaCategory_NameTm"] = "köp gezeklik",
            ["Application_CompanyHead_PositionTm"] = "Türkmenistandaky şahamçasynyň müdiri",
            ["Application_CompanyHead_FullName"] = "Mehmet ÇIRAK",
        };
}

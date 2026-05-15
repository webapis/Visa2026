using System.Text.RegularExpressions;
using ClosedXML.Excel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;

static string RepoRoot()
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

static void WriteSeedTemplate(string path)
{
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    using var workbook = new XLWorkbook();
    var ws = workbook.AddWorksheet("Sanawy");

    ws.Cell(1, 1).Value = "Arza №:";
    ws.Cell(1, 2).Value = "{{ds.FullApplicationNumber}}";
    ws.Cell(2, 1).Value = "Sene:";
    ws.Cell(2, 2).Value = "{{ds.ApplicationDateText}}";

    ws.Cell(4, 1).Value = "№";
    ws.Cell(4, 2).Value = "Ady";
    ws.Cell(4, 3).Value = "Pasport";
    ws.Cell(4, 4).Value = "Wiza";
    ws.Cell(4, 5).Value = "Yş hukugy";

    ws.Cell(5, 1).Value = "{{#ds.rows}}";
    ws.Cell(5, 2).Value = "{{.RowNumber}}";
    ws.Cell(5, 3).Value = "{{.Person_FullName}}";
    ws.Cell(5, 4).Value = "{{.Passport_Number}}";
    ws.Cell(5, 5).Value = "{{.Visa_Number}}";
    ws.Cell(5, 6).Value = "{{.WorkPermit_Number}}";
    ws.Cell(6, 1).Value = "{{/ds.rows}}";

    ws.Columns().AdjustToContents();
    workbook.SaveAs(path);
    Console.WriteLine($"Wrote seed template: {path}");
}

static Application BuildSampleApplication(int itemCount)
{
    var application = new Application
    {
        FullApplicationNumber = "3/-433",
        ApplicationDate = new DateTime(2026, 3, 24),
    };

    for (int i = 1; i <= itemCount; i++)
    {
        application.ApplicationItems.Add(new ApplicationItem());
    }

    return application;
}

static async Task RunMergeTest(string templatePath, int itemCount)
{
    var bytes = await File.ReadAllBytesAsync(templatePath);
    var template = new UserReportTemplate
    {
        TemplateOutputFormat = TemplateOutputFormat.Excel,
        ExcelMergeMode = ExcelMergeMode.ItemList,
        RootBoType = UserReportBoType.ApplicationItem,
        TemplateFile = new FileData(),
    };
    template.TemplateFile = new FileData { FileName = Path.GetFileName(templatePath), Content = bytes };

    var extractor = new ExcelTemplatePlaceholderExtractor();
    var placeholders = await extractor.ExtractPlaceholdersAsync(new MemoryStream(bytes));
    Console.WriteLine($"Template: {templatePath}");
    Console.WriteLine($"Placeholders: {placeholders.Count}; #ds.rows row scan...");

    using var scanWb = new XLWorkbook(new MemoryStream(bytes));
    var ws = scanWb.Worksheets.First();
    foreach (var cell in ws.CellsUsed())
    {
        if (cell.GetFormattedString().Contains("{{#ds.rows}}", StringComparison.Ordinal))
            Console.WriteLine($"  {{#ds.rows}} found at row {cell.Address.RowNumber}, col {cell.Address.ColumnNumber}");
    }

    var application = BuildSampleApplication(itemCount);
    var items = UserReportMergeDataHelper.GetActiveApplicationItems(application);

    using var output = new MemoryStream();
    var generator = new ExcelReportGenerator(extractor);
    await generator.GenerateAsync(template, application, output, items);

    var outPath = Path.Combine(Path.GetDirectoryName(templatePath)!, "_merge_test_output.xlsx");
    await File.WriteAllBytesAsync(outPath, output.ToArray());

    using var outWb = new XLWorkbook(outPath);
    var outWs = outWb.Worksheets.First();
    int filledRows = 0;
    int tokenRows = 0;
    for (int r = 1; r <= outWs.LastRowUsed()?.RowNumber(); r++)
    {
        var rowText = string.Join(" ", outWs.Row(r).CellsUsed().Select(c => c.GetFormattedString()));
        if (rowText.Contains("{{", StringComparison.Ordinal))
            tokenRows++;
        if (rowText.Contains("RowNumber", StringComparison.Ordinal) || Regex.IsMatch(rowText, @"\b[1-9]\d*\b"))
            filledRows++;
    }

    Console.WriteLine($"Wrote: {outPath}");
    Console.WriteLine($"Rows with filled test data: {filledRows} (expected {itemCount})");
    Console.WriteLine($"Rows still containing '{{{{': {tokenRows}");
}

var command = args.Length > 0 ? args[0] : "test-gurlusyk";
var repo = RepoRoot();
var seedPath = Path.Combine(repo, "Visa2026.Module", "Resources", "Templates", "Excel", "Personnel_List_Seed.xlsx");
var gurlusykPath = Path.Combine(repo, "Visa2026.Module", "Resources", "Templates", "Excel", "gurlusyk_uzt.xlsx");

if (command is "generate-seed")
    WriteSeedTemplate(seedPath);

if (command is "test-gurlusyk" or "test")
{
    if (!File.Exists(gurlusykPath))
        throw new FileNotFoundException(gurlusykPath);
    await RunMergeTest(gurlusykPath, 18);
}

if (command is "test-personnel")
    await RunMergeTest(seedPath, 18);

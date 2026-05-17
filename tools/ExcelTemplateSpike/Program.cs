using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using ExcelDataReader;
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

/// <summary>
/// Apply text edits with ClosedXML only. Do not repack .xlsx with ZipFile.CreateFromDirectory — that corrupts OOXML and breaks ClosedXML (NRE in LoadSpreadsheetDocument).
/// </summary>
static void PatchGurlusykEducationPlaceholder(string templatePath)
{
    if (!File.Exists(templatePath))
        throw new FileNotFoundException(templatePath);

    const string from = "{{.Education_LevelTm}}";
    const string to = "{{.Education_LevelAndInstitutionTm}}";

    using var workbook = new XLWorkbook(templatePath);
    foreach (var ws in workbook.Worksheets)
    {
        foreach (var cell in ws.CellsUsed())
        {
            var text = cell.GetFormattedString();
            if (string.IsNullOrEmpty(text) || !text.Contains(from, StringComparison.Ordinal))
                continue;
            cell.Value = text.Replace(from, to, StringComparison.Ordinal);
        }
    }

    workbook.Save();
    Console.WriteLine($"Patched '{from}' -> '{to}' in: {templatePath}");
}

/// <summary>Set <c>P4</c> placeholder for <c>Möhleti we gezekligi</c> column (wrapped text).</summary>
static void PatchGurlusykMohletColumn(string templatePath)
{
    if (!File.Exists(templatePath))
        throw new FileNotFoundException(templatePath);

    using var workbook = new XLWorkbook(templatePath);
    var ws = workbook.Worksheet(1);
    var cell = ws.Cell("P4");
    cell.Value = "{{.Visa_DurationFrequencyBlock}}";
    cell.Style.Alignment.WrapText = true;
    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    workbook.Save();
    Console.WriteLine($"Set Möhleti/gezek placeholder on P4: {templatePath}");
}

static Dictionary<string, string> Sanaw433EkColumnPlaceholders() => new(StringComparer.OrdinalIgnoreCase)
{
    ["no"] = "{{.RowNumber}}",
    ["familiyasy"] = "{{.Person_LastName}}",
    ["ady"] = "{{.Person_FirstName}}",
    ["doglan senesi we yeri"] = "{{.Person_DateOfBirthText}} {{.Person_CountryOfBirthTm}}/ {{.Person_BirthPlace}}",
    ["jynsy"] = "{{.Person_GenderTm}}",
    ["rayatlygy"] = "{{.Person_NationalityCode}}",
    ["pasport belgisi we mohleti"] = "{{.Passport_Number}} {{.Passport_ExpirationDateText}}",
    ["bilimi we okan yeri"] = "{{.Education_LevelAndInstitutionTm}}",
    ["bilimine gora hunari"] = "{{.Education_SpecialtyTm}}",
    ["wezipesi"] = "{{.Position_PositionTm}}",
    ["mohleti we gezekligi"] = "{{.Visa_StartDateText}} {{.Visa_ExpirationDateText}} ({{.Visa_Number}}) {{.Visa_CategoryTm}}",
    ["turkmenistandaky salgysy"] = "{{.Address_FullAddress}}",
    ["dasary yurtdaky salgysy"] = "{{.Person_ForeignAddressWithCountry}}",
    ["barjak hereket cagi"] = "{{.WorkPermit_WorkPermittedLocations}}",
    ["barjak serhet yakasy"] = "{{.Application_BorderZoneLocation_NameTm}}",
};

static string NormalizeHeaderKey(string? text)
{
    if (string.IsNullOrWhiteSpace(text)) return string.Empty;
    if (text.Contains('№', StringComparison.Ordinal)) return "no";
    var s = text.Trim().ToLowerInvariant();
    s = s.Replace('ý', 'y').Replace('ü', 'u').Replace('ö', 'o').Replace('ş', 's')
        .Replace('ç', 'c').Replace('ä', 'a').Replace('ň', 'n').Replace('ž', 'z');
    s = Regex.Replace(s, @"\s+", " ");
    return s;
}

static void Build433EkFromXls(string xlsPath, string xlsxPath)
{
    if (!File.Exists(xlsPath))
        throw new FileNotFoundException(xlsPath);

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    using var stream = File.OpenRead(xlsPath);
    using var reader = ExcelReaderFactory.CreateReader(stream);
    var dataSet = reader.AsDataSet();
    var table = dataSet.Tables[0];

    int headerRow = -1;
    for (int r = 0; r < table.Rows.Count && r < 20; r++)
    {
        for (int c = 0; c < table.Columns.Count; c++)
        {
            if (NormalizeHeaderKey(table.Rows[r][c]?.ToString()) == "familiyasy")
            {
                headerRow = r;
                break;
            }
        }
        if (headerRow >= 0) break;
    }

    if (headerRow < 0)
        throw new InvalidOperationException("Could not find header row (Familiýasy) in 433-ek.xls");

    int dataRow = headerRow + 1;
    int loopMarkerCol = -1;
    var colToPlaceholder = new Dictionary<int, string>();

    for (int c = 0; c < table.Columns.Count; c++)
    {
        var key = NormalizeHeaderKey(table.Rows[headerRow][c]?.ToString());
        if (string.IsNullOrEmpty(key)) continue;
        if (Sanaw433EkColumnPlaceholders().TryGetValue(key, out var ph))
            colToPlaceholder[c] = ph;
    }

    if (colToPlaceholder.Count < 10)
        throw new InvalidOperationException($"Only matched {colToPlaceholder.Count} columns; check header row {headerRow + 1}.");

    loopMarkerCol = colToPlaceholder.Keys.Min();
    while (loopMarkerCol > 0 && colToPlaceholder.ContainsKey(loopMarkerCol - 1))
        loopMarkerCol--;
    if (colToPlaceholder.ContainsKey(loopMarkerCol))
        loopMarkerCol = colToPlaceholder.Keys.Min() - 1;
    if (loopMarkerCol < 0)
        loopMarkerCol = 0;

    using var workbook = new XLWorkbook();
    var ws = workbook.AddWorksheet(table.TableName.Length > 0 ? table.TableName : "Sanawy");

    for (int r = 0; r < table.Rows.Count; r++)
    {
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var val = table.Rows[r][c];
            if (val == null || val == DBNull.Value) continue;
            var text = val.ToString();
            if (string.IsNullOrEmpty(text)) continue;
            ws.Cell(r + 1, c + 1).Value = text;
        }
    }

    foreach (var (col, placeholder) in colToPlaceholder)
    {
        var cell = ws.Cell(dataRow + 1, col + 1);
        cell.Value = placeholder;
        cell.Style.Alignment.WrapText = true;
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
    }

    ws.Cell(dataRow + 1, loopMarkerCol + 1).Value = "{{#ds.rows}}";

    int footerRow = dataRow + 2;
    if (footerRow <= table.Rows.Count)
    {
        for (int c = 0; c < table.Columns.Count; c++)
        {
            var t = table.Rows[footerRow][c]?.ToString();
            if (string.Equals(t?.Trim(), "Ministr", StringComparison.OrdinalIgnoreCase))
            {
                ws.Cell(footerRow + 1, c + 1).Value = "{{.CompanyHead_PositionTm}}";
            }
            else if (t?.Contains("Saparow", StringComparison.OrdinalIgnoreCase) == true
                     || t?.Contains("A.", StringComparison.Ordinal) == true)
            {
                ws.Cell(footerRow + 1, c + 1).Value = "{{.CompanyHead_FullName}}";
            }
        }
    }

    Directory.CreateDirectory(Path.GetDirectoryName(xlsxPath)!);
    workbook.SaveAs(xlsxPath);

    Console.WriteLine($"Built: {xlsxPath}");
    Console.WriteLine($"  Header row: {headerRow + 1}, data/loop row: {dataRow + 1}, columns wired: {colToPlaceholder.Count}");
    foreach (var (col, ph) in colToPlaceholder.OrderBy(kv => kv.Key))
        Console.WriteLine($"    col {col + 1}: {ph}");
}

var command = args.Length > 0 ? args[0] : "test-gurlusyk";
var repo = RepoRoot();
var gurlusykPath = Path.Combine(repo, "Visa2026.Module", "Resources", "Templates", "Excel", "433_gurlusyk_uzt.xlsx");
var xls433Path = Path.Combine(repo, "Visa2026.Module", "Resources", "Templates", "Excel", "433-ek.xls");
var xlsx433Path = Path.Combine(repo, "Visa2026.Module", "Resources", "Templates", "Excel", "433-ek_uzt.xlsx");

if (command is "test-gurlusyk" or "test")
{
    if (!File.Exists(gurlusykPath))
        throw new FileNotFoundException(gurlusykPath);
    await RunMergeTest(gurlusykPath, 18);
}

if (command is "patch-gurlusyk")
    PatchGurlusykEducationPlaceholder(gurlusykPath);

if (command is "patch-gurlusyk-mohlet")
    PatchGurlusykMohletColumn(gurlusykPath);

if (command is "build-433-ek")
    Build433EkFromXls(xls433Path, xlsx433Path);

if (command is "test-433-ek")
{
    if (!File.Exists(xlsx433Path))
        Build433EkFromXls(xls433Path, xlsx433Path);
    await RunMergeTest(xlsx433Path, 3);
}

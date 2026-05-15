using ClosedXML.Excel;

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

var seedPath = Path.Combine(RepoRoot(), "Visa2026.Module", "Resources", "Templates", "Excel", "Personnel_List_Seed.xlsx");
WriteSeedTemplate(seedPath);

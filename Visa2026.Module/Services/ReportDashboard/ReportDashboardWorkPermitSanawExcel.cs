using System.Collections.Generic;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>Sanaw workbook. Rows are the table the officer is looking at.</summary>
public static class ReportDashboardWorkPermitSanawExcel
{
    public const string TitleText = "Daşary ýurt raýatlarynyň sanawy";

    public static List<string> Cells(ReportDashboardPreviewRow row, int rowNumber)
    {
        var cells = new List<string> { rowNumber.ToString() };
        if (row.SanawCells != null)
            cells.AddRange(row.SanawCells);
        return cells;
    }
}
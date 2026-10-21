using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// uzatılanlar company sheet. Açıklama stays empty. The day-count color is not used.
/// </summary>
public static class ReportDashboardVisaUzatilanlarExcel
{
    public static readonly string[] Headers =
    [
        "S.No",
        "Adı Soyadı",
        "Görevi",
        "Projesi",
        "Firması",
        "Mevcut Vize bitiş tarihi",
        "Davetiye için yazı No",
        "İlgili Bakanlığın Başvuru tarihi",
        "Başvurulan vize süresi",
        "İlgili Bakanlıktan çıkan tarih ve numarası",
        "Yabancı Şubeye veriliş tarihi",
        "Yabancı Şube CO No ve tarihi",
        "Şu anki Durumu",
        "Açıklama",
    ];

    public static readonly double[] Widths = [8, 22, 38, 22, 18, 16, 16, 16, 14, 24, 18, 20, 22, 16];

    public static List<string> Cells(ReportDashboardPreviewRow row, int rowNumber)
    {
        var cells = row.UzatilanlarCells.ToList();
        while (cells.Count < Headers.Length - 1)
            cells.Add(string.Empty);
        if (cells.Count > Headers.Length - 1)
            cells = cells.Take(Headers.Length - 1).ToList();
        if (cells.Count > 10)
            cells[10] = StripNumberMark(cells[10]);
        cells.Insert(0, rowNumber.ToString(CultureInfo.InvariantCulture));
        return cells;
    }

    private static string StripNumberMark(string text)
    {
        var lines = new List<string>();
        foreach (var raw in (text ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = raw.Trim().TrimStart('№').Trim();
            if (value.Length > 0)
                lines.Add(value);
        }

        return string.Join("\n", lines);
    }
}

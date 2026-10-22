using System.Collections.Generic;
using System.Globalization;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Çalışma izni listesi workbook. Header is row 1. Dates are Excel dates dd.MM.yyyy.
/// The day count is a number. NOT stays empty.
/// </summary>
public static class ReportDashboardWorkPermitCalismaExcel
{
    public static readonly string[] Headers =
    [
        "S.No",
        "PROJECT",
        "Firması",
        "Adı Soyadı",
        "Doğum Tarihi",
        "Mevcut Pasaport No",
        "Pasaport döneminin bitiş tarihi",
        "Mesleki Görevleri",
        "Çalışma İzni bittiği tarih",
        "Vize Numarası",
        "Vizenin bitiş tarihi",
        "Vize bitmesine kalan süre",
        "Mevcut Adresi",
        "NOT",
    ];

    /// <summary>aktiv personeller only — includes Vize Süresi after Mesleki Görevleri.</summary>
    public static readonly string[] AktivPersonellerHeaders =
    [
        "S.No",
        "PROJECT",
        "Firması",
        "Adı Soyadı",
        "Doğum Tarihi",
        "Mevcut Pasaport No",
        "Pasaport döneminin bitiş tarihi",
        "Mesleki Görevleri",
        "Vize Süresi",
        "Çalışma İzni bittiği tarih",
        "Vize Numarası",
        "Vizenin bitiş tarihi",
        "Vize bitmesine kalan süre",
        "Mevcut Adresi",
        "NOT",
    ];

    public static List<string> Cells(ReportDashboardPreviewRow row, int rowNumber)
    {
        var values = new List<string> { rowNumber.ToString(CultureInfo.InvariantCulture) };
        if (row.CalismaCells != null)
            values.AddRange(row.CalismaCells);
        while (values.Count < Headers.Length)
            values.Add(string.Empty);
        if (values.Count > Headers.Length)
            values.RemoveRange(Headers.Length, values.Count - Headers.Length);
        return values;
    }
}

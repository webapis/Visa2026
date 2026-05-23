namespace Visa2026.DataImporter;

/// <summary>Reads lookup sheet rows using <see cref="ExcelMappings"/> column maps.</summary>
public static class LookupSheetPayloadReader
{
    public static List<Dictionary<string, object?>> Read(string filePath, SheetMap sheetMap)
    {
        if (!File.Exists(filePath))
            return new List<Dictionary<string, object?>>();

        var headerRow = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: false, sheetName: sheetMap.SheetName
        ).FirstOrDefault();

        if (headerRow == null)
            return new List<Dictionary<string, object?>>();

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerRow.Count; i++)
        {
            var text = headerRow[i]?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(text))
                headerIndex[text] = i;
        }

        var payloads = new List<Dictionary<string, object?>>();
        var dataRows = ExcelParser.Parse<List<object>?>(
            filePath, row => row, hasHeader: true, sheetName: sheetMap.SheetName);

        foreach (var row in dataRows)
        {
            if (row == null || row.All(c => c == null || string.IsNullOrWhiteSpace(c?.ToString())))
                continue;

            var firstCell = row[0]?.ToString()?.Trim() ?? "";
            if (firstCell.StartsWith("Start ", StringComparison.OrdinalIgnoreCase)
                || firstCell.StartsWith("End ", StringComparison.OrdinalIgnoreCase))
                continue;

            var payload = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            bool skipRow = false;

            foreach (var colMap in sheetMap.Columns)
            {
                if (!headerIndex.TryGetValue(colMap.Header, out int colIdx))
                    continue;

                var rawValue = colIdx < row.Count ? row[colIdx]?.ToString()?.Trim() ?? "" : "";
                if (rawValue.StartsWith("=DETERMINISTIC_GUID", StringComparison.OrdinalIgnoreCase)
                    || rawValue.StartsWith("<openpyxl", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    if (colMap.Required)
                    {
                        skipRow = true;
                        break;
                    }
                    continue;
                }

                if (colMap.ValueMap != null && colMap.ValueMap.TryGetValue(rawValue, out var mappedValue))
                    rawValue = mappedValue;

                payload[colMap.PayloadProperty] = colMap.Kind switch
                {
                    ColumnKind.Bool => DataParser.IsTextTrue(rawValue),
                    ColumnKind.StringValue => rawValue,
                    ColumnKind.LookupByName => rawValue,
                    _ => DataParser.ParseScalar(rawValue),
                };
            }

            if (!skipRow && payload.Count > 0)
                payloads.Add(payload);
        }

        return payloads;
    }
}

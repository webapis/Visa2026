using System;
using System.Collections.Generic;
using System.IO;
using ExcelDataReader;

namespace Visa2026.DataImporter;

public static class ExcelParser
{
    static ExcelParser()
    {
        // Required for ExcelDataReader to support older encodings
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public static List<string> GetSheetNames(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}");

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var sheetNames = new List<string>();
        do
        {
            sheetNames.Add(reader.Name);
        } while (reader.NextResult());

        return sheetNames;
    }

    public static IEnumerable<T> Parse<T>(string filePath, Func<List<object>, T> mapper, bool hasHeader = true, int skipRows = 0, string? sheetName = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Excel file not found: {filePath}");

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        // If a specific sheet is requested, navigate to it
        if (!string.IsNullOrEmpty(sheetName))
        {
            bool found = false;
            do
            {
                if (string.Equals(reader.Name, sheetName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            } while (reader.NextResult());

            if (!found)
            {
                Console.WriteLine($"[Excel Warning] Sheet '{sheetName}' not found in {filePath}");
                yield break;
            }
        }

        int rowIndex = 0;

        // Loop through rows
        while (reader.Read())
        {
            // 1. Handle row skipping
            if (rowIndex < skipRows)
            {
                rowIndex++;
                continue;
            }

            // 2. Handle Header
            if (hasHeader && rowIndex == skipRows)
            {
                rowIndex++;
                continue;
            }

            // 3. Read Cell Values
            var rowValues = new List<object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                rowValues.Add(reader.GetValue(i));
            }

            T item = default!;
            try
            {
                item = mapper(rowValues);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Excel Warning] Row {rowIndex + 1}: {ex.Message}");
            }

            if (item != null)
                yield return item;

            rowIndex++;
        }
    }
}
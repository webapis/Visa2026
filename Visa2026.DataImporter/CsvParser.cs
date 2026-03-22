using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Visa2026.DataImporter;

public static class CsvParser
{
    /// <summary>
    /// Reads a CSV file line-by-line and maps each row to an object of type T.
    /// Assumes the first row contains headers.
    /// Does not support newlines embedded within quoted fields.
    /// </summary>
    public static IEnumerable<T> Parse<T>(string filePath, Func<Dictionary<string, string>, T> mapper, string? logFilePath = "csv_import_errors.log")
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        using var reader = new StreamReader(filePath);

        // 1. Read Header
        string? headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            yield break;

        var headers = ParseLine(headerLine);
        int rowNum = 1; // Header is row 1

        // 2. Read Data Rows
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseLine(line);
            var rowData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Map headers to values
            for (int i = 0; i < headers.Count && i < values.Count; i++)
            {
                rowData[headers[i]] = values[i];
            }

            T item = default!;
            try
            {
                item = mapper(rowData);
            }
            catch (Exception ex)
            {
                LogParseError(logFilePath, rowNum, ex.Message);
                continue;
            }

            if (item != null)
                yield return item;
        }
    }

    /// <summary>
    /// Reads a CSV file line-by-line and maps each row to an object of type T using column indices.
    /// </summary>
    public static IEnumerable<T> Parse<T>(string filePath, Func<List<string>, T> mapper, bool hasHeader = true, string? logFilePath = "csv_import_errors.log")
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"CSV file not found: {filePath}");

        using var reader = new StreamReader(filePath);

        int rowNum = 0;
        if (hasHeader)
        {
            reader.ReadLine(); // Skip header
            rowNum++;
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            rowNum++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = ParseLine(line);

            T item = default!;
            try
            {
                item = mapper(values);
            }
            catch (Exception ex)
            {
                LogParseError(logFilePath, rowNum, ex.Message);
                continue;
            }

            if (item != null)
                yield return item;
        }
    }

    private static void LogParseError(string? logFilePath, int rowNum, string message)
    {
        string msg = $"[CSV Warning] Error parsing row {rowNum}: {message}";
        Console.WriteLine(msg);

        if (!string.IsNullOrEmpty(logFilePath))
        {
            try
            {
                File.AppendAllText(logFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {msg}{Environment.NewLine}");
            }
            catch { /* Ignore logging errors to allow import to proceed */ }
        }
    }

    private static List<string> ParseLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Handle escaped quotes ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        result.Add(current.ToString());
        return result;
    }
}
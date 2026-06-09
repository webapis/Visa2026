using System.Text;
using System.Text.Json;
using Visa2026.Module.BusinessObjects.Operations;

namespace Visa2026.Module.Services.RuntimeLogging;

public static class ApplicationRuntimeLogCursorInboxFileHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static bool TryWriteInboxFile(
        ApplicationRuntimeLog row,
        string inboxDirectory,
        bool skipIfExists,
        string? sourceSlot,
        string? sourceDatabase,
        out string? writtenPath)
    {
        writtenPath = null;
        if (row == null)
            throw new ArgumentNullException(nameof(row));

        Directory.CreateDirectory(inboxDirectory);

        var inboxFile = Path.Combine(inboxDirectory, $"{row.ID:D}.json");
        if (skipIfExists && File.Exists(inboxFile))
            return false;

        var document = CursorRuntimeErrorInboxDocument.FromRow(row, sourceSlot, sourceDatabase);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(inboxFile, json, Encoding.UTF8);

        var jsonlPath = Path.Combine(inboxDirectory, "inbox.jsonl");
        File.AppendAllText(jsonlPath, json + Environment.NewLine, Encoding.UTF8);

        writtenPath = inboxFile;
        return true;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Visa2026.DataImporter;

public record BatchOperation(HttpMethod Method, string RelativeUrl, object? Payload = null);
public record BatchResultItem(int StatusCode, string Body, string ContentId);

public class BatchResponse
{
    public List<BatchResultItem> Responses { get; } = new();
    public bool HasErrors => Responses.Any(r => r.StatusCode >= 400);
}

public static class ODataBatch
{
    public static HttpContent CreateContent(string baseUrl, IEnumerable<BatchOperation> operations)
    {
        var batchId = "batch_" + Guid.NewGuid().ToString("N");
        var changesetId = "changeset_" + Guid.NewGuid().ToString("N");
        var sb = new StringBuilder();

        // 1. Start Changeset within Batch
        sb.AppendLine($"--{batchId}");
        sb.AppendLine($"Content-Type: multipart/mixed; boundary={changesetId}");
        sb.AppendLine();

        int contentId = 1;
        foreach (var op in operations)
        {
            sb.AppendLine($"--{changesetId}");
            sb.AppendLine("Content-Type: application/http");
            sb.AppendLine("Content-Transfer-Encoding: binary");
            sb.AppendLine($"Content-ID: {contentId++}");
            sb.AppendLine();

            // Request Line: METHOD URL HTTP/1.1
            // URL must be absolute or relative to the service root
            var url = op.RelativeUrl.StartsWith("http") 
                ? op.RelativeUrl 
                : $"{baseUrl.TrimEnd('/')}/{op.RelativeUrl.TrimStart('/')}";

            sb.AppendLine($"{op.Method} {url} HTTP/1.1");
            sb.AppendLine("Content-Type: application/json; charset=utf-8");
            sb.AppendLine("Accept: application/json");
            sb.AppendLine();

            if (op.Payload != null)
            {
                var json = JsonSerializer.Serialize(op.Payload);
                sb.AppendLine(json);
            }
            sb.AppendLine();
        }

        // 2. End Changeset
        sb.AppendLine($"--{changesetId}--");

        // 3. End Batch
        sb.AppendLine($"--{batchId}--");

        var content = new StringContent(sb.ToString(), Encoding.UTF8);
        content.Headers.Remove("Content-Type");
        content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/mixed; boundary={batchId}");
        
        return content;
    }

    public static async Task<BatchResponse> ParseResponseAsync(HttpResponseMessage response)
    {
        var result = new BatchResponse();
        var contentType = response.Content.Headers.ContentType?.ToString();
        
        if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/mixed"))
        {
            // Fallback for non-multipart responses (unlikely for OData batch unless global error)
            var body = await response.Content.ReadAsStringAsync();
            result.Responses.Add(new BatchResultItem((int)response.StatusCode, body, "0"));
            return result;
        }

        var boundary = GetBoundary(contentType);
        var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        bool insideResponse = false;
        int currentStatus = 0;
        string currentBody = "";
        string currentContentId = "";

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.Contains(boundary))
            {
                if (insideResponse)
                {
                    result.Responses.Add(new BatchResultItem(currentStatus, currentBody.Trim(), currentContentId));
                    currentBody = "";
                    currentStatus = 0;
                    currentContentId = "";
                    insideResponse = false;
                }
                
                if (line.EndsWith("--")) break; // End of batch
                continue;
            }

            if (line.StartsWith("HTTP/1.1 "))
            {
                var parts = line.Split(' ');
                if (parts.Length > 1 && int.TryParse(parts[1], out int status))
                {
                    currentStatus = status;
                    insideResponse = true;
                }
            }
            else if (line.StartsWith("Content-ID: ", StringComparison.OrdinalIgnoreCase))
            {
                currentContentId = line.Substring(12).Trim();
            }
            else if (insideResponse && !line.StartsWith("Content-Type:") && !string.IsNullOrWhiteSpace(line))
            {
                currentBody += line;
            }
        }

        return result;
    }

    private static string GetBoundary(string contentType)
    {
        var parts = contentType.Split(';');
        var boundaryPart = parts.FirstOrDefault(p => p.Trim().StartsWith("boundary="));
        return boundaryPart?.Split('=')[1].Trim() ?? "";
    }
}
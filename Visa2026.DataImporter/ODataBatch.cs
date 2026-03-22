using System.Text;
using System.Text.Json;

namespace Visa2026.DataImporter;

public record BatchOperation(HttpMethod Method, string RelativeUrl, object? Payload = null);

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
}
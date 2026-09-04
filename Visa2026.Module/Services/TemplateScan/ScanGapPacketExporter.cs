#nullable enable

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

public interface IScanGapPacketExporter
{
    byte[] ExportJson(ScanGapPacketRequest request);

    byte[] ExportMarkdown(ScanGapPacketRequest request);
}

public sealed class ScanGapPacketRequest
{
    public required Guid ApplicationProfileId { get; init; }

    public Guid? ApplicationProfileInstanceId { get; init; }

    public required string ScanContentSha256 { get; init; }

    public required ScanFieldPlan FieldPlan { get; init; }

    public TemplateValidationReport? Validation { get; init; }

    public required string PlaybookFingerprint { get; init; }

    public required string PlaceholderSetFingerprint { get; init; }

    public string? TemplateName { get; init; }

    public string? ScanFileName { get; init; }

    public string? ProfileName { get; init; }

    public ApplicationProfileTemplateCatalogScope CatalogScope { get; init; } =
        ApplicationProfileTemplateCatalogScope.ProfileSpecific;

    public ApplicationProfileTemplateDataScope DataScope { get; init; } =
        ApplicationProfileTemplateDataScope.ApplicationHeader;
}

public sealed class ScanGapPacketExporter : IScanGapPacketExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public byte[] ExportJson(ScanGapPacketRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var document = BuildDocument(request);
        return JsonSerializer.SerializeToUtf8Bytes(document, JsonOptions);
    }

    public byte[] ExportMarkdown(ScanGapPacketRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Encoding.UTF8.GetBytes(BuildMarkdown(request));
    }

    public static string ComputeContentSha256(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    private static ScanGapPacketDocument BuildDocument(ScanGapPacketRequest request)
    {
        var plan = request.FieldPlan;
        return new ScanGapPacketDocument
        {
            ApplicationProfileId = request.ApplicationProfileId,
            ApplicationProfileInstanceId = request.ApplicationProfileInstanceId,
            TemplateName = request.TemplateName,
            ProfileName = request.ProfileName,
            ScanFileName = request.ScanFileName,
            ScanContentSha256 = request.ScanContentSha256,
            PlaybookFingerprint = request.PlaybookFingerprint,
            PlaceholderSetFingerprint = request.PlaceholderSetFingerprint,
            DataScope = request.DataScope.ToString(),
            ScanKind = plan.ScanKind.ToString(),
            CatalogScope = request.CatalogScope.ToString(),
            FieldPlanSource = plan.Source,
            Gaps = plan.Gaps.Select(static g => new ScanGapPacketGapEntry
            {
                FieldId = g.FieldId,
                LabelText = g.LabelText,
                SuggestedPropertyName = g.SuggestedPropertyName,
            }).ToList(),
            MappedFields = plan.Fields
                .Where(static f => !string.IsNullOrWhiteSpace(f.ProposedToken))
                .Select(static f => new ScanGapPacketMappedField
                {
                    FieldId = f.FieldId,
                    LabelText = f.LabelText,
                    Token = f.ProposedToken,
                    PageIndex = f.PageIndex,
                    Confidence = f.Confidence.ToString(),
                    Scope = f.Scope.ToString(),
                    Box = FormatBox(f.Box),
                }).ToList(),
            UnmappedDetectedFields = plan.Fields
                .Where(static f => string.IsNullOrWhiteSpace(f.ProposedToken))
                .Select(static f => new ScanGapPacketUnmappedField
                {
                    FieldId = f.FieldId,
                    LabelText = f.LabelText,
                    PageIndex = f.PageIndex,
                    Confidence = f.Confidence.ToString(),
                    Box = FormatBox(f.Box),
                }).ToList(),
            ValidationIssues = (request.Validation?.Issues ?? Array.Empty<TemplateValidationIssue>())
                .Select(static i => new ScanGapPacketValidationIssue
                {
                    Severity = i.Severity.ToString(),
                    Message = i.Message,
                    Placeholder = i.Token,
                }).ToList(),
            ExcludedPlaceholders = plan.PlaceholderSet.Excluded
                .Select(static e => new ScanGapPacketExcludedPlaceholder
                {
                    ShortCode = e.ShortCode,
                    Reason = e.Reason.ToString(),
                }).ToList(),
        };
    }

    private static string BuildMarkdown(ScanGapPacketRequest request)
    {
        var doc = BuildDocument(request);
        var sb = new StringBuilder();
        sb.AppendLine("# Template scan gap packet");
        sb.AppendLine();
        sb.AppendLine("## Context");
        sb.AppendLine($"- Profile: {doc.ProfileName ?? doc.ApplicationProfileId.ToString()} (`{doc.ApplicationProfileId}`)");
        if (doc.ApplicationProfileInstanceId is { } instanceId)
            sb.AppendLine($"- Instance: `{instanceId}`");
        sb.AppendLine($"- Template name: {doc.TemplateName ?? "(unspecified)"}");
        sb.AppendLine($"- Scan file: {doc.ScanFileName ?? "(unknown)"}");
        sb.AppendLine($"- Scan SHA-256: `{doc.ScanContentSha256}`");
        sb.AppendLine($"- Data scope: {doc.DataScope}");
        sb.AppendLine($"- Scan kind: {doc.ScanKind}");
        sb.AppendLine($"- Catalog scope: {doc.CatalogScope}");
        sb.AppendLine($"- Playbook fingerprint: `{doc.PlaybookFingerprint}`");
        sb.AppendLine($"- Placeholder set fingerprint: `{doc.PlaceholderSetFingerprint}`");
        sb.AppendLine($"- Field plan source: {doc.FieldPlanSource}");
        sb.AppendLine();

        sb.AppendLine("## Gaps (no library token)");
        if (doc.Gaps.Count == 0)
        {
            sb.AppendLine("_No explicit gaps recorded._");
        }
        else
        {
            var index = 1;
            foreach (var gap in doc.Gaps)
            {
                sb.AppendLine($"{index}. **{gap.LabelText}** (`{gap.FieldId}`)");
                if (!string.IsNullOrWhiteSpace(gap.SuggestedPropertyName))
                    sb.AppendLine($"   - Suggested property: `{gap.SuggestedPropertyName}`");
                index++;
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Unmapped detected fields");
        if (doc.UnmappedDetectedFields.Count == 0)
            sb.AppendLine("_None._");
        else
        {
            foreach (var field in doc.UnmappedDetectedFields)
            {
                sb.AppendLine($"- **{field.LabelText}** — page {field.PageIndex + 1}, {field.Box}, confidence {field.Confidence}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Mapped fields");
        if (doc.MappedFields.Count == 0)
            sb.AppendLine("_None._");
        else
        {
            foreach (var field in doc.MappedFields)
                sb.AppendLine($"- **{field.LabelText}** → `{field.Token}` (page {field.PageIndex + 1}, {field.Box})");
        }
        sb.AppendLine();

        sb.AppendLine("## Validation");
        if (doc.ValidationIssues.Count == 0)
            sb.AppendLine("_No validation issues._");
        else
        {
            foreach (var issue in doc.ValidationIssues)
            {
                var token = string.IsNullOrWhiteSpace(issue.Placeholder) ? string.Empty : $" `{issue.Placeholder}`";
                sb.AppendLine($"- **{issue.Severity}**{token}: {issue.Message}");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Excluded placeholders (profile set)");
        if (doc.ExcludedPlaceholders.Count == 0)
            sb.AppendLine("_None._");
        else
        {
            foreach (var excluded in doc.ExcludedPlaceholders)
                sb.AppendLine($"- `{excluded.ShortCode}` — {excluded.Reason}");
        }

        return sb.ToString();
    }

    private static string FormatBox(ScanBoundingBox box)
    {
        var clamped = box.Clamp();
        return FormattableString.Invariant(
            $"left={clamped.Left:0.###}, top={clamped.Top:0.###}, right={clamped.Right:0.###}, bottom={clamped.Bottom:0.###}");
    }

    private sealed class ScanGapPacketDocument
    {
        public string SchemaVersion { get; init; } = "1";

        public Guid ApplicationProfileId { get; init; }

        public Guid? ApplicationProfileInstanceId { get; init; }

        public string? TemplateName { get; init; }

        public string? ProfileName { get; init; }

        public string? ScanFileName { get; init; }

        public required string ScanContentSha256 { get; init; }

        public required string PlaybookFingerprint { get; init; }

        public required string PlaceholderSetFingerprint { get; init; }

        public required string DataScope { get; init; }

        public required string ScanKind { get; init; }

        public required string CatalogScope { get; init; }

        public required string FieldPlanSource { get; init; }

        public required IReadOnlyList<ScanGapPacketGapEntry> Gaps { get; init; }

        public required IReadOnlyList<ScanGapPacketMappedField> MappedFields { get; init; }

        public required IReadOnlyList<ScanGapPacketUnmappedField> UnmappedDetectedFields { get; init; }

        public required IReadOnlyList<ScanGapPacketValidationIssue> ValidationIssues { get; init; }

        public required IReadOnlyList<ScanGapPacketExcludedPlaceholder> ExcludedPlaceholders { get; init; }
    }

    private sealed class ScanGapPacketGapEntry
    {
        public required string FieldId { get; init; }

        public required string LabelText { get; init; }

        public string? SuggestedPropertyName { get; init; }
    }

    private sealed class ScanGapPacketMappedField
    {
        public required string FieldId { get; init; }

        public required string LabelText { get; init; }

        public string? Token { get; init; }

        public int PageIndex { get; init; }

        public required string Confidence { get; init; }

        public required string Scope { get; init; }

        public required string Box { get; init; }
    }

    private sealed class ScanGapPacketUnmappedField
    {
        public required string FieldId { get; init; }

        public required string LabelText { get; init; }

        public int PageIndex { get; init; }

        public required string Confidence { get; init; }

        public required string Box { get; init; }
    }

    private sealed class ScanGapPacketValidationIssue
    {
        public required string Severity { get; init; }

        public required string Message { get; init; }

        public string? Placeholder { get; init; }
    }

    private sealed class ScanGapPacketExcludedPlaceholder
    {
        public required string ShortCode { get; init; }

        public required string Reason { get; init; }
    }
}

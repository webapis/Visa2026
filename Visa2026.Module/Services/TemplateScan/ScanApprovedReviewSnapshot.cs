#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using Visa2026.Module.Services.TemplateConvert;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Approved Review mapping persisted on the catalog row. Review placeholders restores this
/// instead of re-guessing yellows.
/// </summary>
public sealed class ScanApprovedReviewSnapshot
{
    public const int CurrentVersion = 1;

    public const string FieldPlanSource = "approved-review";

    public int Version { get; set; } = CurrentVersion;

    public List<ScanApprovedReviewFieldDto> Fields { get; set; } = [];

    public static string Serialize(ScanFieldPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var locked = ScanFieldPlanOfficerOverride.LockMapped(plan);
        var dto = new ScanApprovedReviewSnapshot
        {
            Version = CurrentVersion,
            Fields = locked.Fields.Select(ToDto).ToList(),
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static ScanApprovedReviewSnapshot? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ScanApprovedReviewSnapshot>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ScanApprovedReviewFieldDto ToDto(ScanDetectedField field) =>
        new()
        {
            FieldId = field.FieldId,
            PageIndex = field.PageIndex,
            LabelText = field.LabelText,
            ProposedToken = field.ProposedToken,
            Confidence = field.Confidence,
            Scope = field.Scope,
            IsLocked = field.IsLocked,
            HiddenPartIndexes = field.HiddenPartIndexes.Count == 0
                ? null
                : field.HiddenPartIndexes.ToArray(),
            Region = ToRegionDto(field.SourceRegion),
        };

    private static ScanApprovedReviewRegionDto? ToRegionDto(DocumentRegion? region) =>
        region switch
        {
            DocumentRegion.WordSpan word => new ScanApprovedReviewRegionDto
            {
                Kind = "WordSpan",
                ParagraphAddress = word.ParagraphAddress,
                Start = word.Start,
                Length = word.Length,
            },
            DocumentRegion.WordDrawing drawing => new ScanApprovedReviewRegionDto
            {
                Kind = "WordDrawing",
                ParagraphAddress = drawing.ParagraphAddress,
                DrawingIndex = drawing.DrawingIndex,
                TextInsertOffset = drawing.TextInsertOffset,
            },
            DocumentRegion.ExcelCell cell => new ScanApprovedReviewRegionDto
            {
                Kind = "ExcelCell",
                SheetName = cell.SheetName,
                CellReference = cell.CellReference,
            },
            _ => null,
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };
}

public sealed class ScanApprovedReviewFieldDto
{
    public string FieldId { get; set; } = string.Empty;

    public int PageIndex { get; set; }

    public string LabelText { get; set; } = string.Empty;

    public string? ProposedToken { get; set; }

    public ScanFieldConfidence Confidence { get; set; }

    public ScanFieldScope Scope { get; set; }

    public bool IsLocked { get; set; }

    public int[]? HiddenPartIndexes { get; set; }

    public ScanApprovedReviewRegionDto? Region { get; set; }
}

public sealed class ScanApprovedReviewRegionDto
{
    public string Kind { get; set; } = string.Empty;

    public string? ParagraphAddress { get; set; }

    public int Start { get; set; }

    public int Length { get; set; }

    public int DrawingIndex { get; set; }

    public int TextInsertOffset { get; set; }

    public string? SheetName { get; set; }

    public string? CellReference { get; set; }

    public DocumentRegion? ToRegion() =>
        Kind switch
        {
            "WordSpan" => new DocumentRegion.WordSpan(ParagraphAddress ?? string.Empty, Start, Length),
            "WordDrawing" => new DocumentRegion.WordDrawing(
                ParagraphAddress ?? string.Empty,
                DrawingIndex,
                TextInsertOffset),
            "ExcelCell" => new DocumentRegion.ExcelCell(SheetName ?? string.Empty, CellReference ?? string.Empty),
            _ => null,
        };
}
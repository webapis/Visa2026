#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Layout families for yellow-mark guessing. Analyze tries every applicable pattern;
/// sample names are never hardcoded.
/// </summary>
public enum ScanGuessingPatternKind
{
    /// <summary>Ministry letter: No + date, urgency, N (words), N (words) ay, gezeklik.</summary>
    OfficialLetter = 1,

    /// <summary>Borcnama-style: left label, yellow on the line, parenthetical caption under the line.</summary>
    CaptionUnderLine = 2,

    /// <summary>Sahsy kagyzy-style: short field name on the left, yellow value on the right (no caption).</summary>
    LeftLabelForm = 3,

    /// <summary>Labor-contract prose: yellow inside a sentence or Isgar / Is beriji footer column.</summary>
    InlineProse = 4,

    /// <summary>Excel sanaw: column header above a yellow data cell.</summary>
    ExcelColumnHeader = 5,
}
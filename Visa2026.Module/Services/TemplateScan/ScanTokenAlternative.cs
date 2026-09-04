#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>One ranked placeholder guess for a yellow mark (manual / header / shape inference).</summary>
public sealed record ScanTokenAlternative(
    string Token,
    string ShortCode,
    int ScorePercent,
    string Reason);

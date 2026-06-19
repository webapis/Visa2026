namespace Visa2026.Tools.CarboneSpike;

internal enum SpikeScenario
{
    GurlusykExcel,
    SanawWord,
    Forma16Word,
}

internal static class SpikeScenarioParser
{
    public static SpikeScenario Parse(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "gurlusyk" or "gurlusyk-excel" or "excel" => SpikeScenario.GurlusykExcel,
        "sanaw" or "sanaw-word" or "word" => SpikeScenario.SanawWord,
        "forma16" or "forma-16" or "forma16-word" => SpikeScenario.Forma16Word,
        _ => throw new ArgumentException($"Unknown scenario '{value}'. Use: gurlusyk, sanaw, forma16."),
    };

    public static string TemplateRelativePath(SpikeScenario scenario) => scenario switch
    {
        SpikeScenario.GurlusykExcel => Path.Combine("Excel", "433_gurlusyk_uzt.xlsx"),
        SpikeScenario.SanawWord => "Sanaw_uzt.docx",
        SpikeScenario.Forma16Word => "Forma_16.docx",
        _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
    };
}
